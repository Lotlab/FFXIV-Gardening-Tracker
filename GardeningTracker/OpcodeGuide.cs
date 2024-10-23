using GardeningTracker.Packets;
using Lotlab.PluginCommon;
using Lotlab.PluginCommon.FFXIV.Parser;
using Lotlab.PluginCommon.FFXIV.Parser.Packets;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

namespace GardeningTracker
{
    class OpcodeGuide
    {
        NetworkParser parser = new NetworkParser(false);
        public SimpleLogger logger { get; }
        public GardeningData data { get; }
        
        Dictionary<Type, (bool, uint)> opcodeResult = new Dictionary<Type, (bool, uint)>();

        List<string> opcodeComments = new List<string>();

        List<HandlerItem> handlers = new List<HandlerItem>();

        object indexLock = new object();
        
        int currentIndex = -1;
        HandlerItem current => currentIndex < handlers.Count && currentIndex >= 0 ? handlers[currentIndex] : null;

        public OpcodeGuide(SimpleLogger logger, GardeningData data)
        {
            this.logger = logger;
            this.data = data;
            Init();
        }

        public void Init()
        {
            AddHandler<GuessZoneInto, FFXIVIpcGuessZoneInto>(true, "请传送至住宅区", (obj) =>
            {
                var dat = obj as GuessZoneInto;

                // todo: 与已知服务器名称比较
                bool eof = false;
                bool valid = true;
                for (int i = 0; i < 32; i++)
                {
                    byte c = dat.Value.serverName[i];
                    if (eof)
                    {
                        if (c != 0) valid = false;
                    }
                    else
                    {
                        if (c == 0) { eof = true; }
                        else if (!Char.IsLetterOrDigit((char)c)) valid = false;
                    }
                }

                if (valid)
                    logger.LogDebug("ServName: " + dat.ServerName);

                return valid;
            });

            AddHandler<ObjectExteralData, FFXIVIpcObjectExternalData>(true, "请传送至住宅区", (obj) =>
            {
                var dat = obj as ObjectExteralData;

                var ext = parser.ParseAsPacket<ObjectExternalDataLand>(dat.Value.data);
                for (int i = 0; i < 8; i++)
                {
                    if (ext.Seed[i] != 0)
                    {
                        if (ext.Seed[i] > 97) return false;
                        if (ext.State[i] < 1 || ext.State[i] > 10) return false;
                    } 
                    else
                    {
                        if (ext.State[i] != 0) return false;
                    }
                }

                return true;
            });

            AddHandler<ObjectSpawn, FFXIVIpcObjectSpawn>(true, "请传送至住宅区", (obj) =>
            {
                // 判断园圃
                var dat = obj as ObjectSpawn;
                return data.IsGarden(dat.Value.objId, out _, out _);
            });

            AddHandler<ActorControlSelf, FFXIVIpcActorControlSelf>(true, "请在休息区等待休息经验加成上涨", (obj) => {
                var dat = obj as ActorControlSelf;
                if (dat.Category != FFXIVIpcActorControlType.UpdateRestedExp)
                    return false;

                logger.LogDebug(dat.ToString());
                if (dat.Param[0] > 604800)
                    return false;

                for (int i = 1; i < dat.Param.Length; i++)
                {
                    if (dat.Param[i] != 0)
                        return false;
                }

                return true;
            });

            AddHandler<ItemInfo, FFXIVIpcItemInfo>(true, "请打开陆行鸟鞍囊", (obj) =>
            {
                var dat = obj as ItemInfo;

                // 是否为陆行鸟按囊
                return dat.Value.containerId == 4000;
            });

            AddHandler<InventoryModify, FFXIVIpcInventoryModifyHandler>(false, "请将背包内的任意物品移动到陆行鸟鞍囊中", (obj) =>
            {
                var dat = obj as InventoryModify;
                if (dat.Value.toContainer >= 4000 && dat.Value.toContainer <= 4003)
                {
                    var discardCode = dat.Value.action - 1;
                    logger.LogInfo("Inventory move operation code: " + dat.Value.action);
                    opcodeComments.Add("InventoryModifyCode: " + discardCode);
                    return true;
                }
                return false;
            });

            AddHandler<UpdateInventorySlot, FFXIVIpcItemInfo>(true, "请购买一个鱼粉", (obj) =>
            {
                var dat = obj as UpdateInventorySlot;
                // 防止物品信息重新出现
                if (opcodeResult.TryGetValue(typeof(ItemInfo), out var itemOpcode) && dat.Value.ipc.type == itemOpcode.Item2) return false;
                return dat.Value.catalogId == 7767; // 鱼粉
            });

            AddHandler<EventStart, FFXIVIpcEventStart>(true, "请钓鱼并立刻收杆", (obj) =>
            {
                var dat = obj as EventStart;
                return dat.Value.targetID == 0x150001;
            });

            AddHandler<GuessTargetAction, FFXIVIpcGuessTargetAction>(false, "请护理菜地", (obj) =>
            {
                var dat = obj as GuessTargetAction;
                logger.LogDebug($"[Opcode Guide] {dat.Value}");
                return dat.Value.operation == 2;
            });

            AddHandler<GuessTargetAction16, FFXIVIpcGuessTargetAction16>(false, "请使用在背包内的肥料对菜地施肥", (obj) =>
            {
                var dat = obj as GuessTargetAction16;
                logger.LogDebug($"[Opcode Guide] {dat.Value}");
                var fertParam = parser.ParseAsPacket<TargetAction16Fertilize>(dat.Value.param);
                return fertParam.unknown1 == 1 && fertParam.fertilizer.containerID <= 4;
            });

            AddHandler<GuessTargetAction32, FFXIVIpcGuessTargetAction32>(false, "请使用军票兑换探险币", (obj) =>
            {
                var dat = obj as GuessTargetAction32;
                logger.LogDebug($"[Opcode Guide] {dat.Value}");
                return BitConverter.ToUInt32(dat.Value.param, 8) == 21072; // 探险币
            });
        }

        public void NetworkReceive(byte[] message)
        {
            if (current?.Inbound != true) return;
            handlePacket(message);
        }

        public void NetworkSend(byte[] message)
        {
            if (current?.Inbound != false) return;
            handlePacket(message);
        }

        private void handlePacket(byte[] message)
        {
            int now = currentIndex;
            if (handlePacket(message, current))
            {
                Next(now);
            }
        }

        bool handlePacket(byte[] message, HandlerItem item)
        {
            try
            {
                // size check
                if (message.Length != Marshal.SizeOf(item.Type.Item2)) return false;

                // header check
                var header = parser.ParseIPCHeader(message);
                if (header == null) return false;

                var data = parser.ByteArrayToStructure(item.Type.Item2, message);
                var obj = parser.ParsePacketAs(item.Type.Item1, item.Type.Item2, message);

                if (item.Callback(obj))
                {
                    opcodeResult[item.Type.Item1] = (item.Inbound, header.Value.type);
                    parser.SetOpcode(item.Type.Item1.Name, header.Value.type);
                    logger.LogInfo($"{item.Type.Item1.Name} Opcode: {header.Value.type}");
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                logger.LogDebug(e.ToString());
                return false;
            }
        }

        class HandlerItem
        {
            public Tuple<Type, Type> Type;
            public string Instruction;
            public bool Inbound;
            public Func<object, bool> Callback;
        }

        /// <summary>
        /// 重新开始Opcode助手
        /// </summary>
        public void Restart()
        {
            opcodeResult.Clear();
            opcodeComments.Clear();

            lock (indexLock) currentIndex = -1;
            Next(currentIndex);
        }

        /// <summary>
        /// 跳过这个Opcode
        /// </summary>
        public void Skip()
        {
            if (currentIndex >= 0)
                Next(currentIndex);
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        public void Save()
        {
            string content = "";
            foreach (var item in opcodeComments)
                content += $"// {item}\n";
            
            foreach (var item in opcodeResult)
            {
                if (item.Value.Item1 == false)
                    content += $"{item.Key.Name} = {item.Value.Item2},\n";
            }

            content += "// rx\n";

            foreach (var item in opcodeResult)
            {
                if (item.Value.Item1 == true)
                    content += $"{item.Key.Name} = {item.Value.Item2},\n";
            }

            try
            {
                const string fileName = "GardeningTrackerOpcode.txt";
                File.WriteAllText(fileName, content);
                logger.LogInfo("文件已保存至 " + Path.GetFullPath(fileName));
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
        }

        /// <summary>
        /// 跳转到下一项
        /// </summary>
        /// <param name="now"></param>
        void Next(int now)
        {
            lock (indexLock)
            {
                if (now < currentIndex) return;

                currentIndex++;
                if (current != null)
                    logger.LogInfo($"[{currentIndex + 1}/{handlers.Count}] {current.Instruction}");
                if (currentIndex >= handlers.Count)
                    logger.LogInfo("Opcode 已检测完毕。");
            }
        }

        void AddHandler<T, T2>(bool inbound, string instruction, Func<object, bool> callback) where T : IPCPacketBase<T2>, new() where T2 : struct
        {
            handlers.Add(new HandlerItem()
            {
                Type = new Tuple<Type, Type>(typeof(T), typeof(T2)),
                Instruction = instruction,
                Inbound = inbound,
                Callback = callback
            });
            parser.AddType<T, T2>();
        }
    }
}
