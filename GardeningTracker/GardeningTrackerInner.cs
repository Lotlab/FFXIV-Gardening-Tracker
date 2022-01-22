using Lotlab;
using System;
using System.Collections.Generic;

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

    class GardeningTrackerInner
    {
        PluginControlViewModel viewModel { get; }
        Config config { get; }

        SimpleLogger logger { get; }

        GardeningData data { get; } = new GardeningData();

        GardeningStorage storage { get; } = new GardeningStorage();

        Action<string> actLog { get; }

        public GardeningTrackerInner(PluginControlViewModel vm, Config config, SimpleLogger logger, Action<string> actLogFunc)
        {
            viewModel = vm;
            this.config = config;
            this.logger = logger;
            this.actLog = actLogFunc;

            data.Read();
        }

        /// <summary>
        /// ActorID 映射表
        /// </summary>
        Dictionary<uint, FFXIVIpcObjectSpawn> ActorIDTable = new Dictionary<uint, FFXIVIpcObjectSpawn>();

        /// <summary>
        /// TargetID -> ActorID 映射表
        /// </summary>
        Dictionary<uint, uint> TargetIDTable = new Dictionary<uint, uint>();

        /// <summary>
        /// 物体映射表
        /// </summary>
        Dictionary<FFXIVItemIndexer, FFXIVIpcItemInfo> ItemTable = new Dictionary<FFXIVItemIndexer, FFXIVIpcItemInfo>();

        /// <summary>
        /// 当前区域
        /// </summary>
        CurrentZone currentZone = null;

        void clearActorTable()
        {
            ActorIDTable.Clear();
            TargetIDTable.Clear();
            ItemTable.Clear();
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
                logger.LogError(e);
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
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
        }

        /// <summary>
        /// 解析区域切换信息
        /// </summary>
        /// <param name="ipc"></param>
        private void parseZoneSwitch(FFXIVIpcPacket ipc)
        {
            // 切换区域
            var zoneInto = new FFXIVIpcGuessZoneInto(ipc.Data);
            logger.LogTrace(zoneInto.ToString());

            // 清空交互信息
            clearActorTable();

            // 解析区域信息
            if (zoneInto.House.WorldId != 0xFFFF)
            {
                currentZone = new CurrentZone(zoneInto.House, true);
            }
            else if (zoneInto.Area.WorldId != 0xFFFF)
            {
                currentZone = new CurrentZone(zoneInto.Area, false);
            }
            else
            {
                currentZone = null;
            }

            var zoneName = GetZoneName(currentZone);
            viewModel.CurrentZone = zoneName;
            logger.LogInfo($"切换区域: {zoneName}");
        }

        /// <summary>
        /// 获取区域名称
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        private string GetZoneName(CurrentZone zone)
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

            logger.LogTrace(cf.ToString());
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
            lock (ActorIDTable)
            {
                ActorIDTable[obj.ActorId] = obj;
            }
        }

        /// <summary>
        /// 获取TargetID对应的Object
        /// </summary>
        /// <param name="targetID"></param>
        /// <returns></returns>
        FFXIVIpcObjectSpawn getTargetObject(uint targetID)
        {
            // 查找对应的ActorID
            if (!TargetIDTable.ContainsKey(targetID))
                return null;

            var actorID = TargetIDTable[targetID];

            // 查找对应的Object
            if (!ActorIDTable.ContainsKey(actorID))
                return null;

            var obj = ActorIDTable[actorID];

            // 判断是否为目标物体
            if (!data.IsGarden(obj.ObjId, out _, out _))
                return null;

            return obj;
        }

        GardeningIdent getTargetGardeningIdent(uint targetID)
        {
            var obj = getTargetObject(targetID);
            if (obj == null || currentZone == null) return null;

            var zoneIdent = currentZone.Ident;
            if (!currentZone.IsInHouse)
                zoneIdent.LandId = obj.HousingLandID;

            return new GardeningIdent(zoneIdent, obj.ActorId, obj.ObjId, obj.HousingObjIndex, obj.HousingObjSubIndex);
        }

        private string getPotPos(GardeningIdent obj)
        {
            data.IsGarden(obj.ObjectID, out var potName, out var isPot);
            var potPos = $"{obj.LandIndex + 1}";
            if (!isPot) potPos += $",{obj.LandSubIndex + 1}";

            var pos = data.GetZoneName(obj.House, currentZone.IsInHouse);
            return $"{pos} {potName}({potPos})";
        }

        /// <summary>
        /// 解析物体互动信息
        /// </summary>
        /// <param name="ipc"></param>
        private void parseObjectInteractive(FFXIVIpcPacket ipc)
        {
            var act = new FFXIVIpcGuessTargetAction(ipc.Data);
            logger.LogTrace(act.ToString());

            var obj = getTargetGardeningIdent(act.TargetID);
            if (obj == null) return;
            
            string action;
            switch (act.Operation)
            {
                case 0:
                    action = "查看";
                    break;
                case 1:
                    action = "收获";
                    storage.Remove(obj);
                    WriteActLog(obj, GardenOperation.Harvest);
                    break;
                case 2:
                    action = "护理";
                    storage.Care(obj, ipc.Timestamp);
                    WriteActLog(obj, GardenOperation.Care);
                    break;
                case 3:
                    action = "处理";
                    storage.Remove(obj);
                    WriteActLog(obj, GardenOperation.Dispose);
                    break;
                default:
                    action = $"未知操作({act.Operation})";
                    break;
            }

            string potPos = getPotPos(obj);
            logger.LogInfo($"{action}了 {potPos} 的作物");
        }

        private void parseTargetAction1(FFXIVIpcPacket ipc)
        {
            var action = new FFXIVIpcGuessTargetAction1(ipc.Data);
            logger.LogTrace(action.ToString());

            var obj = getTargetGardeningIdent(action.TargetID);
            if (obj == null) return;

            // 解析施肥操作
            var fertParam = new FFXIVIpcActionParamFertilize(action.Param);
            logger.LogTrace(fertParam.ToString());

            // 查找施肥物体
            var itemIndex = new FFXIVItemIndexer(fertParam.Fertilizer);
            if (!ItemTable.ContainsKey(itemIndex))
            {
                logger.LogDebug($"无法找到施放的肥料：{fertParam.Fertilizer}");
                return;
            }

            // 记录到存储区
            var fertilizerID = ItemTable[itemIndex].CatalogId;
            storage.Fertilize(obj, fertilizerID, ipc.Timestamp);

            // 写日志
            if (!data.FertilizerNames.ContainsKey(fertilizerID))
            {
                logger.LogDebug($"无法找到肥料({fertilizerID})名称");
                return;
            }
            logger.LogInfo($"对位于 {getPotPos(obj)} 的作物施了 {data.FertilizerNames[fertilizerID]}");
            WriteActLog(obj, GardenOperation.Fertilize, fertilizerID);
        }


        private void parseTargetAction2(FFXIVIpcPacket ipc)
        {
            var action = new FFXIVIpcGuessAction2(ipc.Data);
            logger.LogTrace(action.ToString());

            var obj = getTargetGardeningIdent(action.TargetID);
            if (obj == null) return;

            // 解析播种操作
            var seedParam = new FFXIVIpcActionParamSowing(action.Param);
            logger.LogTrace(seedParam.ToString());

            // 查找播种物体
            var soilIndex = new FFXIVItemIndexer(seedParam.Soil);
            var seedIndex = new FFXIVItemIndexer(seedParam.Seed);
            if (!ItemTable.ContainsKey(soilIndex) || !ItemTable.ContainsKey(seedIndex))
            {
                logger.LogDebug($"无法找到播种对应的物体： {soilIndex}, {seedIndex}");
                return;
            }

            // 记录到存储区
            var soilObjID = ItemTable[soilIndex].CatalogId;
            var seedObjID = ItemTable[seedIndex].CatalogId;
            storage.Sowing(new GardeningItem(obj, soilObjID, seedObjID, ipc.Timestamp));

            // 写日志
            if (!data.SeedNames.ContainsKey(seedObjID) || !data.SoilNames.ContainsKey(soilObjID))
            {
                logger.LogDebug($"无法找到种子({seedObjID})或土壤({soilObjID})名称");
                return;
            }

            var soilName = data.SoilNames[soilObjID];
            var seedName = data.SeedNames[seedObjID];
            string potPos = getPotPos(obj);

            logger.LogInfo($"将{seedName}种植在了位于 {potPos} 的{soilName}中");
            WriteActLog(obj, GardenOperation.Sow, soilObjID, seedObjID);
        }

        private void parseTargetSelection(FFXIVIpcPacket ipc)
        {
            // 选择目标
            var bd = new FFXIVIpcGuessTargetBinding(ipc.Data);
            logger.LogTrace(bd.ToString());
        }

        private void WriteActLog(GardeningIdent ident, GardenOperation op, uint param1 = 0, uint param2 = 0)
        {
            // 写Binary数据
            string actorID = BitConverter.GetBytes(ident.ActorID).ToHexString();
            string objID = BitConverter.GetBytes(ident.ObjectID).ToHexString();
            string housingLink = BitConverter.GetBytes((ident.LandIndex << 16) + ident.LandSubIndex).ToHexString();
            var str = $"00|{DateTime.Now.ToString("O")}|0|GardeningTracker|{ident.House.ToHexString()}|{actorID}|{objID}|{housingLink}|{(int)op}|{param1}|{param2}||";
            actLog(str);

            // 写可读数据
            string param1Name = "";
            string param2Name = "";
            if (op == GardenOperation.Sow)
            {
                param1Name = data.SoilNames[param1];
                param2Name = data.SeedNames[param2];
            } 
            else if (op == GardenOperation.Fertilize)
            {
                param1Name = data.FertilizerNames[param1];
            }

            var str2 = $"00|{DateTime.Now.ToString("O")}|0|GardeningTracker|{getPotPos(ident)}|{op}|{param1Name}|{param2Name}||";
            actLog(str2);
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
