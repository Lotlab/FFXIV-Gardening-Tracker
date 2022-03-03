using Lotlab.PluginCommon;
using System;
using System.Collections.Generic;
using System.IO;
using Lotlab.PluginCommon.FFXIV.Parser;
using GardeningTracker.Packets;
using Lotlab.PluginCommon.FFXIV.Parser.Packets;

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
        public Config Config { get; }

        public SimpleLogger Logger { get; }

        GardeningData data { get; }

        NetworkParser parser { get; } = new NetworkParser();

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

        public string DataPath { get; }

        public GardeningTracker(Action<string> actLogFunc, string extDataPath, string appDataPath)
        {
            logInAct = actLogFunc;
            DataPath = appDataPath;

            Logger = new SimpleLoggerSync(Path.Combine(DataPath, "app.log"));

            // Load config
            try
            {
                Config = new Config(Path.Combine(DataPath, "config.json"));
                Config.Load();
            }
            catch (Exception e)
            {
                Logger.LogError("配置加载失败: " + e.Message);
            }

            Logger.SetFilter(Config.LogLevel);

            // load opcode
            try
            {
                var loader = new WizardOpcodeReader();
                var opcode = loader.ReadFile(Path.Combine(extDataPath, "opcode.txt"));
                parser.SetOpcodes(opcode);
            }
            catch (Exception e)
            {
                Logger.LogError("Opcode加载失败: " + e.Message);
            }

            // Read external data
            try
            {
                data = new GardeningData(extDataPath);
                data.Read();
            }
            catch (Exception e)
            {
                Logger.LogError("数据文件加载失败: " + e.Message);
            }
            Logger.LogInfo("数据文件加载成功");

            // Init storage data
            try
            {
                Storage = new GardeningStorage(data, Config, Path.Combine(DataPath, "garden.json"));
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
        Dictionary<uint, ObjectSpawn> ActorIDTable = new Dictionary<uint, ObjectSpawn>();

        /// <summary>
        /// 物体额外信息存储表
        /// </summary>
        Dictionary<UInt32, ObjectExteralData> ObjectExternalDataTable = new Dictionary<uint, ObjectExteralData>();

        /// <summary>
        /// TargetID -> ActorID 映射表
        /// </summary>
        Dictionary<uint, uint> TargetIDTable = new Dictionary<uint, uint>();

        /// <summary>
        /// 物体映射表
        /// </summary>
        Dictionary<FFXIVItemShort, FFXIVIpcItemInfo> ItemTable = new Dictionary<FFXIVItemShort, FFXIVIpcItemInfo>();

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
                var packet = parser.ParsePacket(message);
                if (packet == null) return;

                switch (packet)
                {
                    case GuessTargetBinding t1: // 目标选择
                        parseTargetSelection(t1);
                        break;
                    case GuessTargetAction t2: // 目标互动
                        parseObjectInteractive(t2);
                        break;
                    case GuessTargetAction16 t3: // 目标互动1，施肥
                        parseTargetAction1(t3);
                        break;
                    case GuessTargetAction32 t4: // 目标互动2，播种
                        parseTargetAction2(t4);
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
                var packet = parser.ParsePacket(message);
                if (packet == null) return;

                switch (packet)
                {
                    case ObjectSpawn t1: // 物体生成
                        parseObjectSpawn(t1);
                        break;
                    case ItemInfo t2: // 物品信息
                        parseItemInfo(t2.Value);
                        break;
                    case UpdateInventorySlot t6: // 更新物品信息
                        parseItemInfo(t6.Value);
                        break;
                    case GuessTargetConfirm t3: // 目标确认
                        parseTargetConfirm(t3);
                        break;
                    case GuessZoneInto t4: // 场景切换
                        parseZoneSwitch(t4);
                        break;
                    case ObjectExteralData t5: // 额外数据
                        parseObjectExternalData(t5);
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
        private void parseObjectExternalData(ObjectExteralData ext)
        {
            Logger.LogTrace(ext.ToString());

            lock (ObjectExternalDataTable)
            {
                ObjectExternalDataTable[ext.Value.housingLink & 0x0000FFFF] = ext;
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
            var landSubID = (housingLink >> 24) & 0xFF;
            if (!ObjectExternalDataTable.ContainsKey(indexLink)) return 0;

            var dat = ObjectExternalDataTable[indexLink].Value.data;
            var landDat = parser.ParseAsPacket<ObjectExternalDataLand>(dat);
            Logger.LogTrace(landDat.ToString());

            var index = landDat.Seed[landSubID];

            return data.GetSeedIdByIndex(index);
        }

        /// <summary>
        /// 解析区域切换信息
        /// </summary>
        /// <param name="ipc"></param>
        private void parseZoneSwitch(GuessZoneInto zoneInto)
        {
            // 切换区域
            Logger.LogTrace(zoneInto.ToString());

            // 清空交互信息
            clearActorTable();

            // 解析区域信息
            if (zoneInto.House.worldId != 0xFFFF)
            {
                CurrentZone = new CurrentZone(new FFXIVLandIdent(zoneInto.House), true);
            }
            else if (zoneInto.Area.worldId != 0xFFFF)
            {
                CurrentZone = new CurrentZone(new FFXIVLandIdent(zoneInto.Area), false);
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

        private void parseTargetConfirm(GuessTargetConfirm cf)
        {
            // 接收到交互确认信息
            // 存储物体的TargetID与ActorID的对应关系
            TargetIDTable[cf.Value.targetID] = cf.Value.actorID;

            Logger.LogTrace(cf.ToString());
        }

        /// <summary>
        /// 解析并存储物体信息
        /// </summary>
        /// <param name="ipc"></param>
        private void parseItemInfo(FFXIVIpcItemInfo item)
        {
            lock (ItemTable)
            {
                ItemTable[new FFXIVItemShort() { containerID = item.containerId, slotID = item.slot }] = item;
            }
        }

        /// <summary>
        /// 接收到可交互物体信息
        /// 存储物体ActorID与物体本身对应关系
        /// </summary>
        /// <param name="ipc"></param>
        private void parseObjectSpawn(ObjectSpawn obj)
        {
            // Logger.LogTrace(obj.ToString());

            lock (ActorIDTable)
            {
                ActorIDTable[obj.Value.actorId] = obj;
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
            if (!data.IsGarden(obj.Value.objId, out _, out var isPot))
                return null;

            var zoneIdent = CurrentZone.Ident;
            if (!isPot)
                zoneIdent.LandId = obj.HousingLandID;

            return new GardeningIdent(zoneIdent, obj.Value.objId, obj.Value.housingLink, isPot);
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
        private void parseObjectInteractive(GuessTargetAction act)
        {
            Logger.LogTrace(act.ToString());
            var obj = getTargetGardeningIdent(act.Value.targetID);
            if (obj == null) return;

            var guessSeed = GetSeedID(obj.HousingLink);

            string action;
            switch (act.Value.operation)
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
                    Storage.Care(obj, act.Value.ipc.timestamp, guessSeed);
                    actLogOperation(obj, GardenOperation.Care);
                    break;
                case 3:
                    action = "处理";
                    Storage.Remove(obj);
                    actLogOperation(obj, GardenOperation.Dispose);
                    break;
                default:
                    action = $"未知操作({act.Value.operation})";
                    break;
            }

            string potPos = getPotNamePos(obj);
            Logger.LogInfo($"{action}了 {potPos} 的作物({guessSeed})");
        }

        /// <summary>
        /// 解析物体互动信息
        /// </summary>
        /// <param name="ipc"></param>
        private void parseTargetAction1(GuessTargetAction16 act)
        {
            Logger.LogTrace(act.ToString());
            var obj = getTargetGardeningIdent(act.Value.targetID);
            if (obj == null) return;

            // 解析施肥操作
            var fertParam = parser.ParseAsPacket<TargetAction16Fertilize>(act.Value.param);
            Logger.LogTrace(fertParam.ToString());

            // 查找施肥物体
            var itemIndex = fertParam.fertilizer.GetShort();
            if (!ItemTable.ContainsKey(itemIndex))
            {
                Logger.LogDebug($"无法找到施放的肥料：{fertParam.fertilizer}");
                dumpItemTable(); // Debug
                return;
            }

            // 记录到存储区
            var fertilizerID = ItemTable[itemIndex].catalogId;
            var guessSeed = GetSeedID(obj.HousingLink);
            Storage.Fertilize(obj, fertilizerID, act.Value.ipc.timestamp, guessSeed);

            // 写日志
            Logger.LogInfo($"对位于 {getPotNamePos(obj)} 的作物施了 {data.GetFertilizerName(fertilizerID)}");
            actLogOperation(obj, GardenOperation.Fertilize, fertilizerID);
        }

        /// <summary>
        /// 解析物体互动信息
        /// </summary>
        /// <param name="ipc"></param>
        private void parseTargetAction2(GuessTargetAction32 act)
        {
            Logger.LogTrace(act.ToString());

            var obj = getTargetGardeningIdent(act.Value.targetID);
            if (obj == null) return;

            // 解析播种操作
            var seedParam = parser.ParseAsPacket<TargetAction32Sowing>(act.Value.param);
            Logger.LogTrace(seedParam.ToString());

            // 查找播种物体
            var soilIndex = seedParam.soil.GetShort();
            var seedIndex = seedParam.seed.GetShort();
            if (!ItemTable.ContainsKey(soilIndex) || !ItemTable.ContainsKey(seedIndex))
            {
                if (!ItemTable.ContainsKey(soilIndex))
                    Logger.LogDebug($"无法找到播种对应的土壤： {soilIndex}");
                
                if (!ItemTable.ContainsKey(seedIndex))
                    Logger.LogDebug($"无法找到播种对应的种子： {soilIndex}");

                dumpItemTable(); // Debug
                return;
            }

            // 记录到存储区
            var soilObjID = ItemTable[soilIndex].catalogId;
            var seedObjID = ItemTable[seedIndex].catalogId;
            Storage.Sowing(new GardeningItem(obj, soilObjID, seedObjID, act.Value.ipc.timestamp));

            // 写日志
            var soilName = data.GetSoilName(soilObjID);
            var seedName = data.GetSeedName(seedObjID);
            string potPos = getPotNamePos(obj);

            Logger.LogInfo($"将{seedName}种植在了位于 {potPos} 的{soilName}中");
            actLogOperation(obj, GardenOperation.Sow, soilObjID, seedObjID);
        }

        /// <summary>
        /// 把物品表内的物品全部打印到日志
        /// </summary>
        private void dumpItemTable()
        {
            foreach (var kv in ItemTable)
            {
                var item = kv.Value;
                var str = $"Item {item.catalogId}@({item.containerSequence}, {item.containerId}, {item.slot}), Qty: {item.quantity}, Hq: {item.hqFlag}, Cond: {item.condition}";
                Logger.LogTrace(str);
            }
        }

        private void parseTargetSelection(GuessTargetBinding packet)
        {
            // 选择目标
            Logger.LogTrace(packet.ToString());
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
            var housingLink = (ident.LandSubIndex << 24) + ident.LandIndex;
            writeActLog("00", $"{ident.House.WorldId}|{ident.House.MapId}|{ident.House.WardNum}|{ident.House.LandId}|{ident.ObjectID}|{housingLink}|{(int)op}|{param1}|{param2}|");

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
