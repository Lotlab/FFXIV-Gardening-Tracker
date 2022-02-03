using Lotlab;
using System;
using System.Collections.Generic;
using System.IO;

namespace GardeningTracker
{
    enum GardenOperation
    {
        Sow, // 播种
        Care, // 护理
        Fertilize, // 施肥
        Harvest, // 收获
        Dispose // 处理
    }

    class GardeningTracker : PropertyNotifier
    {
        public Config Config { get; } = new Config();

        public SimpleLogger Logger { get; }

        GardeningData data { get; } = new GardeningData();

        public GardeningStorage Storage { get; }
        Action<string> logInAct { get; }

        public event Action<IEnumerable<GardeningItem>> OnSyncContent;

        bool overlayInited = false;
        public bool OverlayInited
        {
            get => overlayInited;
            set
            {
                overlayInited = value;
                OnPropertyChanged();
            }
        }

        public static string DataPath => Path.Combine(Environment.CurrentDirectory, "AppData", "GardeningTracker");

        void prepareDir()
        {
            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);
        }

        public GardeningTracker(Action<string> actLogFunc)
        {
            logInAct = actLogFunc;

            prepareDir();
            Logger = new SimpleLogger(Path.Combine(DataPath, "app.log"));

            try
            {
                Config.Load();
            }
            catch (Exception e)
            {
                Logger.LogError("配置加载失败: " + e.Message);
            }

            Logger.SetFilter(Config.LogLevel);

            try
            {
                data.Read();
            }
            catch (Exception e)
            {
                Logger.LogError("数据文件加载失败: " + e.Message);
            }
            Logger.LogInfo("数据文件加载成功");

            Storage = new GardeningStorage(data, Config);

            try
            {
                Storage.Load();
            }
            catch (Exception e)
            {
                Logger.LogError("保存的信息加载失败: " + e.Message);
            }

            Logger.LogInfo("已恢复上次保存的信息");
            Logger.LogInfo($"初始化完毕");
        }

        /// <summary>
        /// ActorID 映射表
        /// </summary>
        Dictionary<uint, FFXIVIpcObjectSpawn> ActorIDTable = new Dictionary<uint, FFXIVIpcObjectSpawn>();

        /// <summary>
        /// 物体额外信息存储表
        /// </summary>
        Dictionary<UInt32, FFXIVIpcObjectExternData> ObjectExternalDataTable = new Dictionary<uint, FFXIVIpcObjectExternData>();

        /// <summary>
        /// TargetID -> ActorID 映射表
        /// </summary>
        Dictionary<uint, uint> TargetIDTable = new Dictionary<uint, uint>();

        /// <summary>
        /// 物体映射表
        /// </summary>
        Dictionary<FFXIVItemIndexer, FFXIVIpcItemInfo> ItemTable = new Dictionary<FFXIVItemIndexer, FFXIVIpcItemInfo>();

        CurrentZone _currentZone = null;

        /// <summary>
        /// 当前区域
        /// </summary>
        public CurrentZone CurrentZone
        {
            get => _currentZone;
            private set
            {
                _currentZone = value;
                Logger.LogInfo($"切换区域: {GetZoneName(CurrentZone)}");
                OnPropertyChanged();
            }
        }

        void clearActorTable()
        {
            ActorIDTable.Clear();
            TargetIDTable.Clear();
            ItemTable.Clear();
            // 是否有必要？好像会缓存
            ObjectExternalDataTable.Clear();
        }

        public void NetworkSend(byte[] message)
        {
            try
            {
                if (!FFXIVSegmentPacket.IsIpcSegment(message)) return;

                var segment = new FFXIVSegmentPacket(message);
                var ipc = new FFXIVIpcPacket(segment.Data);

                switch (ipc.Type)
                {
                    case 0x02bd: // 目标选择
                        parseTargetSelection(ipc);
                        break;
                    case 0x02e3: // 目标互动
                        parseObjectInteractive(ipc);
                        break;
                    case 0x0083: // 目标互动1，施肥
                        parseTargetAction1(ipc);
                        break;
                    case 0x01c7: // 目标互动2，播种
                        parseTargetAction2(ipc);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public void NetworkReceive(byte[] message)
        {
            try
            {
                if (!FFXIVSegmentPacket.IsIpcSegment(message)) return;

                var segment = new FFXIVSegmentPacket(message);
                var ipc = new FFXIVIpcPacket(segment.Data);

                switch (ipc.Type)
                {
                    case 0x0306: // 物体生成
                        parseObjectSpawn(ipc);
                        break;
                    case 0x0305: // 物品信息
                        parseItemInfo(ipc);
                        break;
                    case 0x008b: // 目标确认
                        parseTargetConfirm(ipc);
                        break;
                    case 0x0076: // 场景切换
                        parseZoneSwitch(ipc);
                        break;
                    case 0x00E9: // 额外数据
                        parseObjectExternalData(ipc);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public void SystemLogZoneChange(uint world, string area, int ward)
        {
            if (CurrentZone != null) return;

            foreach (var item in data.MapNames)
            {
                if (item.Value == area)
                {
                    var mapID = item.Key;

                    CurrentZone = new CurrentZone(new FFXIVLandIdent()
                    {
                        MapId = (UInt16)mapID,
                        WardNum = (UInt16)(ward - 1),
                        WorldId = (UInt16)world
                    }, false);
                }
            }
        }

        /// <summary>
        /// 解析扩展家具信息
        /// </summary>
        /// <param name="ipc"></param>
        private void parseObjectExternalData(FFXIVIpcPacket ipc)
        {
            var ext = new FFXIVIpcObjectExternData(ipc.Data);
            Logger.LogTrace(ext.ToString());

            lock (ObjectExternalDataTable)
            {
                ObjectExternalDataTable[ext.HousingLink & 0x0000FFFF] = ext;
            }
        }

        /// <summary>
        /// 获取可能的种子ID
        /// </summary>
        /// <param name="housingLink"></param>
        /// <returns></returns>
        uint GetSeedID(uint housingLink)
        {
            var indexLink = housingLink & 0x0000FFFF;
            var landSubID = housingLink >> 24;
            if (!ObjectExternalDataTable.ContainsKey(indexLink)) return 0;

            var dat = ObjectExternalDataTable[indexLink].Data;
            var landDat = new FFXIVLandExternalData(dat);
            Logger.LogTrace(landDat.ToString());

            var index = landDat.Infos[landSubID].Seed;

            return data.GetSeedIdByIndex(index);
        }

        /// <summary>
        /// 解析区域切换信息
        /// </summary>
        /// <param name="ipc"></param>
        private void parseZoneSwitch(FFXIVIpcPacket ipc)
        {
            // 切换区域
            var zoneInto = new FFXIVIpcGuessZoneInto(ipc.Data);
            Logger.LogTrace(zoneInto.ToString());

            // 清空交互信息
            clearActorTable();

            // 解析区域信息
            if (zoneInto.House.WorldId != 0xFFFF)
            {
                CurrentZone = new CurrentZone(zoneInto.House, true);
            }
            else if (zoneInto.Area.WorldId != 0xFFFF)
            {
                CurrentZone = new CurrentZone(zoneInto.Area, false);
            }
            else
            {
                CurrentZone = null;
            }
        }

        /// <summary>
        /// 获取区域名称
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        public string GetZoneName(CurrentZone zone)
        {
            if (zone == null)
                return "未知区域";

            return data.GetZoneName(zone.Ident, zone.IsInHouse);
        }

        private void parseTargetConfirm(FFXIVIpcPacket ipc)
        {
            // 接收到交互确认信息
            // 存储物体的TargetID与ActorID的对应关系
            var cf = new FFXIVIpcGuessTargetConfirm(ipc.Data);
            TargetIDTable[cf.TargetID] = cf.ActorID;

            Logger.LogTrace(cf.ToString());
        }

        /// <summary>
        /// 解析并存储物体信息
        /// </summary>
        /// <param name="ipc"></param>
        private void parseItemInfo(FFXIVIpcPacket ipc)
        {
            var item = new FFXIVIpcItemInfo(ipc.Data);
            lock (ItemTable)
            {
                ItemTable[new FFXIVItemIndexer() { ContainerID = item.ContainerId, SlotID = item.Slot }] = item;
            }
        }

        /// <summary>
        /// 接收到可交互物体信息
        /// 存储物体ActorID与物体本身对应关系
        /// </summary>
        /// <param name="ipc"></param>
        private void parseObjectSpawn(FFXIVIpcPacket ipc)
        {
            var obj = new FFXIVIpcObjectSpawn(ipc.Data);
            // Logger.LogTrace(obj.ToString());

            lock (ActorIDTable)
            {
                ActorIDTable[obj.ActorId] = obj;
            }
        }

        /// <summary>
        /// 获取操作的目标花盆信息
        /// </summary>
        /// <param name="targetID">操作TargetID</param>
        /// <returns></returns>
        GardeningIdent getTargetGardeningIdent(uint targetID)
        {
            if (CurrentZone == null) return null;

            // 查找对应的ActorID
            if (!TargetIDTable.ContainsKey(targetID))
                return null;

            var actorID = TargetIDTable[targetID];

            // 查找对应的Object
            if (!ActorIDTable.ContainsKey(actorID))
                return null;

            var obj = ActorIDTable[actorID];

            // 判断是否为目标物体
            if (!data.IsGarden(obj.ObjId, out _, out var isPot))
                return null;

            var zoneIdent = CurrentZone.Ident;
            if (!isPot)
                zoneIdent.LandId = obj.HousingLandID;

            return new GardeningIdent(zoneIdent, obj.ObjId, obj.HousingLink, isPot);
        }

        /// <summary>
        /// 获取花盆带位置的名字
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private string getPotNamePos(GardeningIdent obj)
        {
            data.IsGarden(obj.ObjectID, out var potName, out var isPot);
            var potPos = $"{obj.LandIndex + 1}";
            if (!isPot) potPos += $",{obj.LandSubIndex + 1}";

            var pos = data.GetZoneName(obj.House, CurrentZone.IsInHouse);
            return $"{pos} {potName}({potPos})";
        }

        /// <summary>
        /// 解析物体互动信息
        /// </summary>
        /// <param name="ipc"></param>
        private void parseObjectInteractive(FFXIVIpcPacket ipc)
        {
            var act = new FFXIVIpcGuessTargetAction(ipc.Data);
            Logger.LogTrace(act.ToString());

            var obj = getTargetGardeningIdent(act.TargetID);
            if (obj == null) return;

            var guessSeed = GetSeedID(obj.HousingLink);

            string action;
            switch (act.Operation)
            {
                case 0:
                    action = "查看";
                    break;
                case 1:
                    action = "收获";
                    Storage.Remove(obj);
                    actLogOperation(obj, GardenOperation.Harvest);
                    break;
                case 2:
                    action = "护理";
                    Storage.Care(obj, ipc.Timestamp, guessSeed);
                    actLogOperation(obj, GardenOperation.Care);
                    break;
                case 3:
                    action = "处理";
                    Storage.Remove(obj);
                    actLogOperation(obj, GardenOperation.Dispose);
                    break;
                default:
                    action = $"未知操作({act.Operation})";
                    break;
            }

            string potPos = getPotNamePos(obj);
            Logger.LogInfo($"{action}了 {potPos} 的作物");
        }

        /// <summary>
        /// 解析物体互动信息
        /// </summary>
        /// <param name="ipc"></param>
        private void parseTargetAction1(FFXIVIpcPacket ipc)
        {
            var action = new FFXIVIpcGuessTargetAction1(ipc.Data);
            Logger.LogTrace(action.ToString());

            var obj = getTargetGardeningIdent(action.TargetID);
            if (obj == null) return;

            // 解析施肥操作
            var fertParam = new FFXIVIpcActionParamFertilize(action.Param);
            Logger.LogTrace(fertParam.ToString());

            // 查找施肥物体
            var itemIndex = new FFXIVItemIndexer(fertParam.Fertilizer);
            if (!ItemTable.ContainsKey(itemIndex))
            {
                Logger.LogDebug($"无法找到施放的肥料：{fertParam.Fertilizer}");
                return;
            }

            // 记录到存储区
            var fertilizerID = ItemTable[itemIndex].CatalogId;
            var guessSeed = GetSeedID(obj.HousingLink);
            Storage.Fertilize(obj, fertilizerID, ipc.Timestamp, guessSeed);

            // 写日志
            Logger.LogInfo($"对位于 {getPotNamePos(obj)} 的作物施了 {data.GetFertilizerName(fertilizerID)}");
            actLogOperation(obj, GardenOperation.Fertilize, fertilizerID);
        }

        /// <summary>
        /// 解析物体互动信息
        /// </summary>
        /// <param name="ipc"></param>
        private void parseTargetAction2(FFXIVIpcPacket ipc)
        {
            var action = new FFXIVIpcGuessAction2(ipc.Data);
            Logger.LogTrace(action.ToString());

            var obj = getTargetGardeningIdent(action.TargetID);
            if (obj == null) return;

            // 解析播种操作
            var seedParam = new FFXIVIpcActionParamSowing(action.Param);
            Logger.LogTrace(seedParam.ToString());

            // 查找播种物体
            var soilIndex = new FFXIVItemIndexer(seedParam.Soil);
            var seedIndex = new FFXIVItemIndexer(seedParam.Seed);
            if (!ItemTable.ContainsKey(soilIndex) || !ItemTable.ContainsKey(seedIndex))
            {
                Logger.LogDebug($"无法找到播种对应的物体： {soilIndex}, {seedIndex}");
                return;
            }

            // 记录到存储区
            var soilObjID = ItemTable[soilIndex].CatalogId;
            var seedObjID = ItemTable[seedIndex].CatalogId;
            Storage.Sowing(new GardeningItem(obj, soilObjID, seedObjID, ipc.Timestamp));

            // 写日志
            var soilName = data.GetSoilName(soilObjID);
            var seedName = data.GetSeedName(seedObjID);
            string potPos = getPotNamePos(obj);

            Logger.LogInfo($"将{seedName}种植在了位于 {potPos} 的{soilName}中");
            actLogOperation(obj, GardenOperation.Sow, soilObjID, seedObjID);
        }

        private void parseTargetSelection(FFXIVIpcPacket ipc)
        {
            // 选择目标
            var bd = new FFXIVIpcGuessTargetBinding(ipc.Data);
            Logger.LogTrace(bd.ToString());
        }

        /// <summary>
        /// 记录操作
        /// </summary>
        /// <param name="ident">关联信息</param>
        /// <param name="op">操作</param>
        /// <param name="param1">参数1</param>
        /// <param name="param2">参数2</param>
        private void actLogOperation(GardeningIdent ident, GardenOperation op, uint param1 = 0, uint param2 = 0)
        {
            // 写Binary数据
            string objID = BitConverter.GetBytes(ident.ObjectID).ToHexString();
            string housingLink = BitConverter.GetBytes((ident.LandSubIndex << 24) + ident.LandIndex).ToHexString();
            writeActLog("00", $"{ident.House.ToHexString()}|{objID}|{housingLink}|{(int)op}|{param1}|{param2}|");

            // 写可读数据
            string param1Name = "";
            string param2Name = "";
            if (op == GardenOperation.Sow)
            {
                param1Name = data.GetSoilName(param1);
                param2Name = data.GetSeedName(param2);
            }
            else if (op == GardenOperation.Fertilize)
            {
                param1Name = data.GetFertilizerName(param1);
            }

            writeActLog("01", $"{getPotNamePos(ident)}|{op}|{param1Name}|{param2Name}|");
        }

        public void SyncContent()
        {
            OnSyncContent?.Invoke(Storage.GetStorageItems());
            Logger.LogInfo("数据已同步");
        }

        private void writeActLog(string type, string content)
        {
            logInAct($"00|{DateTime.Now.ToString("O")}|0|GardeningTracker|{type}|{content}|");
        }

        public void DeInit()
        {
            try
            {
                Storage.Save();
                Config.Save();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            Logger.Close();
        }
    }

    /// <summary>
    /// 当前位置信息
    /// </summary>
    class CurrentZone
    {
        /// <summary>
        /// 是否在房间内
        /// </summary>
        public bool IsInHouse { get; }

        /// <summary>
        /// 位置相关
        /// </summary>
        public FFXIVLandIdent Ident { get; }

        public CurrentZone(FFXIVLandIdent ident, bool inHouse)
        {
            Ident = ident;
            IsInHouse = inHouse;
        }
    }
}
