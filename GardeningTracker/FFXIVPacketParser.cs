using System;
using System.Text;

namespace GardeningTracker
{
    /// <summary>
    /// FFXIV Segment
    /// </summary>
    class FFXIVSegmentPacket
    {
        /// <summary>
        /// Segment 总长度
        /// </summary>
        public Int32 Len { get; }
        /// <summary>
        /// The session ID this segment describes
        /// </summary>
        public Int32 Src { get; }
        /// <summary>
        /// The session ID this packet is being delivered to
        /// </summary>
        public Int32 Dst { get; }
        /// <summary>
        /// The segment type.
        /// </summary>
        public UInt16 Type { get; }

        /// <summary>
        /// Segment body data
        /// </summary>
        public byte[] Data { get; }

        public FFXIVSegmentPacket(byte[] message)
        {
            if (message.Length < 16) throw new ArgumentException("不是一个正确的FFXIV Segment包");
            Len = BitConverter.ToInt32(message, 0);
            Src = BitConverter.ToInt32(message, 4);
            Dst = BitConverter.ToInt32(message, 8);
            Type = BitConverter.ToUInt16(message, 12);
            Data = new byte[Len - 16];
            Array.Copy(message, 16, Data, 0, message.Length - 16);
        }

        /// <summary>
        /// 直接获取SegmentType
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static UInt16 GetSegmentType(byte[] message)
        {
            return BitConverter.ToUInt16(message, 12);
        }

        /// <summary>
        /// 是否为IPC包
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool IsIpcSegment(byte[] message)
        {
            return GetSegmentType(message) == 3;
        }
    }

    /// <summary>
    /// FFXIV IPC 包
    /// </summary>
    class FFXIVIpcPacket
    {
        /// <summary>
        /// Opcode
        /// </summary>
        public UInt16 Type { get; }
        /// <summary>
        /// 服务器ID
        /// </summary>
        public UInt16 ServerID { get; }
        /// <summary>
        /// Unix 时间戳
        /// </summary>
        public UInt32 Timestamp { get; }
        /// <summary>
        /// IPC data
        /// </summary>
        public byte[] Data { get; }

        public FFXIVIpcPacket(byte[] message)
        {
            if (message.Length < 16) throw new ArgumentException("不是一个正确的 FFXIV IPC 包");
            Type = BitConverter.ToUInt16(message, 2);
            ServerID = BitConverter.ToUInt16(message, 6);
            Timestamp = BitConverter.ToUInt32(message, 8);
            Data = new byte[message.Length - 16];
            Array.Copy(message, 16, Data, 0, message.Length - 16);
        }
    }

    /// <summary>
    /// 物品信息
    /// </summary>
    class FFXIVIpcItemInfo
    {
        public UInt32 ContainerSequence { get; }
        // UInt32 unknown;
        /// <summary>
        /// 所在区域ID
        /// </summary>
        public UInt16 ContainerId { get; }
        /// <summary>
        /// 所在格子
        /// </summary>
        public UInt16 Slot { get; }
        /// <summary>
        /// 数量
        /// </summary>
        public UInt32 Quantity { get; }
        /// <summary>
        /// 物体的ID
        /// </summary>
        public UInt32 CatalogId { get; }
        public UInt32 ReservedFlag { get; }
        /// <summary>
        /// 签名者UID
        /// </summary>
        public UInt64 SignatureId { get; }
        public byte HqFlag { get; }
        // byte unknown2;
        public UInt16 Condition { get; }
        public UInt16 SpiritBond { get; }
        public UInt16 Stain { get; }
        public UInt32 GlamourCatalogId { get; }
        public UInt16[] Materias { get; }
        public byte[] Buffers { get; }
        // UInt32 unknown10;

        public FFXIVIpcItemInfo(byte[] message)
        {
            if (message.Length != 64) throw new ArgumentException("不是一个正确的 ItemInfo 包");
            var offset = 0;

            ContainerSequence = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            offset += sizeof(UInt32); // unknown
            ContainerId = BitConverter.ToUInt16(message, offset);
            offset += sizeof(UInt16);
            Slot = BitConverter.ToUInt16(message, offset);
            offset += sizeof(UInt16);
            Quantity = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            CatalogId = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            ReservedFlag = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            SignatureId = BitConverter.ToUInt64(message, offset);
            offset += sizeof(UInt64);
            HqFlag = message[offset++];
            offset++; // unknown2
            Condition = BitConverter.ToUInt16(message, offset);
            offset += sizeof(UInt16);
            SpiritBond = BitConverter.ToUInt16(message, offset);
            offset += sizeof(UInt16);
            Stain = BitConverter.ToUInt16(message, offset);
            offset += sizeof(UInt16);
            GlamourCatalogId = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);

            Materias = new UInt16[5];
            for (int i = 0; i < 5; i++)
            {
                Materias[i] = BitConverter.ToUInt16(message, offset);
                offset += sizeof(UInt16);
            }

            Buffers = new byte[5];
            Array.Copy(message, offset, Buffers, 0, 5);
            offset += 5;

            offset += sizeof(byte); // padding
            offset += sizeof(UInt32); // unknown10

            System.Diagnostics.Debug.Assert(offset == 64);
        }

        public override string ToString()
        {
            return $"Item ID: {CatalogId}, Slot: ({ContainerSequence}, {ContainerId}, {Slot}), Quantity: {Quantity}";
        }
    }

    /// <summary>
    /// 物体生成信息
    /// </summary>
    class FFXIVIpcObjectSpawn
    {
        public byte SpawnIndex { get; }
        public byte ObjKind { get; }
        public byte State { get; }
        // byte unknown3;
        public UInt32 ObjId { get; }
        public UInt32 ActorId { get; }
        public UInt32 LevelId { get; }
        // UInt32 unknown10;
        public UInt32 SomeActorId14 { get; }
        public UInt32 GimmickId { get; }
        public float Scale { get; }
        // Int16 unknown20a;
        public UInt16 Rotation { get; }
        // Int16 unknown24a;
        // Int16 unknown24b;
        public UInt16 Flag { get; }
        // Int16 unknown28c;
        public UInt32 HousingLink { get; }
        public FFXIVPosition3 Position { get; }
        // Int16 unknown3C;
        // Int16 unknown3E;

        /// <summary>
        /// 关联的房屋庭具Index
        /// </summary>
        public byte HousingObjIndex => (byte)(HousingLink & 0xFF);
        /// <summary>
        /// 关联的房屋编号
        /// </summary>
        public byte HousingLandID => (byte)((HousingLink >> 8) & 0xFF);
        /// <summary>
        /// 未知
        /// </summary>
        public byte HousingUnknown => (byte)((HousingLink >> 16) & 0xFF);
        /// <summary>
        /// 关联的庭具子Index
        /// </summary>
        public byte HousingObjSubIndex => (byte)((HousingLink >> 24) & 0xFF);

        public FFXIVIpcObjectSpawn(byte[] message)
        {
            if (message.Length != 64) throw new ArgumentException("不是一个正确的 ObjectSpawn 包");
            var offset = 0;

            SpawnIndex = message[offset++];
            ObjKind = message[offset++];
            State = message[offset++];
            offset++; // unknown3

            ObjId = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            ActorId = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            LevelId = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            offset += sizeof(UInt32); // unknown10
            SomeActorId14 = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            GimmickId = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            Scale = BitConverter.ToSingle(message, offset);
            offset += sizeof(float);
            offset += sizeof(Int16); // unknown20a
            Rotation = BitConverter.ToUInt16(message, offset);
            offset += sizeof(UInt16);
            offset += sizeof(Int16); // unknown24a
            offset += sizeof(Int16); // unknown24b
            Flag = BitConverter.ToUInt16(message, offset);
            offset += sizeof(UInt16);
            offset += sizeof(Int16); // unknown28c
            HousingLink = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            var pos_x = BitConverter.ToSingle(message, offset);
            offset += sizeof(float);
            var pos_y = BitConverter.ToSingle(message, offset);
            offset += sizeof(float);
            var pos_z = BitConverter.ToSingle(message, offset);
            offset += sizeof(float);
            Position = new FFXIVPosition3(pos_x, pos_y, pos_z);
            offset += sizeof(Int16); // unknown3C
            offset += sizeof(Int16); // unknown3E

            System.Diagnostics.Debug.Assert(offset == 64);
        }

        public override string ToString()
        {
            return $"Object Spawn. Index: {SpawnIndex}, Kind: {ObjKind}, State: {State}, ObjID: {ObjId}, ActorID: {ActorId}, LevelID: {LevelId}, HousingLink: ({HousingObjIndex}, {HousingLandID}, {HousingUnknown}, {HousingObjSubIndex}), Pos: {Position}";
        }
    }

    /// <summary>
    /// 位置信息
    /// </summary>
    struct FFXIVPosition3
    {
        public float X;
        public float Y;
        public float Z;

        public FFXIVPosition3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }

    /// <summary>
    /// 可能是选择操作物体绑定
    /// </summary>
    class FFXIVIpcGuessTargetBinding
    {
        public UInt32 ActorID { get; }
        public UInt32 Unknown1 { get; }
        public UInt32 TargetID { get; }
        public UInt32 Unknown2 { get; }

        public FFXIVIpcGuessTargetBinding(byte[] message)
        {
            if (message.Length != 16) throw new ArgumentException();
            var offset = 0;

            ActorID = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            Unknown1 = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            TargetID = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            Unknown2 = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
        }

        public override string ToString()
        {
            return $"(?) Target Bind. ActorID: {ActorID}, TargetID: {TargetID}, Unk1: {Unknown1}";
        }
    }

    /// <summary>
    /// 可能是选择操作物体响应
    /// </summary>
    class FFXIVIpcGuessTargetConfirm
    {
        public UInt32 ActorID { get; }
        public UInt32 Unknown1 { get; }
        public UInt32 TargetID { get; }
        public UInt32 Unknown2 { get; }
        public UInt32 Unknown3 { get; }
        public UInt32 Unknown4 { get; }

        public FFXIVIpcGuessTargetConfirm(byte[] message)
        {
            if (message.Length != 24) throw new ArgumentException();
            var offset = 0;

            ActorID = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            Unknown1 = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            TargetID = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            Unknown2 = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            Unknown3 = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            Unknown4 = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
        }

        public override string ToString()
        {
            return $"(?) Target Confirm. ActorID: {ActorID}, TargetID: {TargetID}, Unk1: {Unknown1}, Unk2: {Unknown2}, Unk3: {Unknown3}, Unk4: {Unknown4}";
        }
    }

    /// <summary>
    /// 可能是操作物体
    /// </summary>
    class FFXIVIpcGuessTargetAction
    {
        public UInt32 TargetID { get; }
        public UInt32 Flags { get; }
        public UInt32 Operation { get; }

        public FFXIVIpcGuessTargetAction(byte[] message)
        {
            if (message.Length != 16) throw new ArgumentException();
            var offset = 0;

            TargetID = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            Flags = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            Operation = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
        }

        public override string ToString()
        {
            return $"(?) Target Operation. TargetID: {TargetID}, Operation: {Operation}, Flags: {Flags}";
        }
    }

    /// <summary>
    /// 可能是需要有两个参数的操作物体
    /// 可能是播种
    /// </summary>
    class FFXIVIpcGuessAction2
    {
        public UInt32 TargetID { get; }
        public UInt32 Unknown1 { get; }
        public UInt32 Unknown2 { get; }

        public byte[] Param { get; }

        public FFXIVIpcGuessAction2(byte[] message)
        {
            if (message.Length != 40) throw new ArgumentException();
            var offset = 0;

            TargetID = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            Unknown1 = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            Unknown2 = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);

            Param = new byte[40 - offset];
            Array.Copy(message, offset, Param, 0, Param.Length);
        }

        public override string ToString()
        {
            return $"(?) Action2. TargetID: {TargetID}, Unk1: {Unknown1}, Unk2: {Unknown2}, Param: {Param.ToHexString()}";
        }
    }

    /// <summary>
    /// 播种用参数
    /// </summary>
    class FFXIVIpcActionParamSowing
    {
        /// <summary>
        /// 土壤
        /// </summary>
        public FFXIVItemIndex Soil { get; }
        /// <summary>
        /// 种子
        /// </summary>
        public FFXIVItemIndex Seed { get; }

        public FFXIVIpcActionParamSowing(byte[] message)
        {
            if (message.Length != 28) throw new ArgumentException();
            var offset = 0;

            var container_id = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            var slot_id = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);

            Soil = new FFXIVItemIndex() { ContainerID = container_id, SlotID = slot_id };

            container_id = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            slot_id = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);

            Seed = new FFXIVItemIndex() { ContainerID = container_id, SlotID = slot_id };
        }

        public override string ToString()
        {
            return $"(?) Sowing. SoilAt: {Soil}, SeedAt: {Seed}";
        }
    }

    /// <summary>
    /// 施肥用参数
    /// </summary>
    class FFXIVIpcActionParamFertilize
    {
        public FFXIVItemIndex Fertilizer { get; }
        public FFXIVIpcActionParamFertilize(byte[] message)
        {
            if (message.Length != 12) throw new ArgumentException();
            var offset = 0;

            var container_id = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            var slot_id = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);

            Fertilizer = new FFXIVItemIndex() { ContainerID = container_id, SlotID = slot_id };
        }

        public override string ToString()
        {
            return $"(?) Fertilize. Fertilizer: {Fertilizer}";
        }
    }

    /// <summary>
    /// 可能是需要有一个参数的操作物体
    /// 施肥会触发这个包
    /// </summary>
    class FFXIVIpcGuessTargetAction1
    {
        public UInt32 TargetID { get; }
        public UInt32 Unknown1 { get; }
        public UInt32 Unknown2 { get; }

        public byte[] Param { get; }

        public FFXIVIpcGuessTargetAction1(byte[] message)
        {
            if (message.Length != 24) throw new ArgumentException();
            var offset = 0;

            TargetID = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            Unknown1 = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);
            Unknown2 = BitConverter.ToUInt32(message, offset);
            offset += sizeof(UInt32);

            Param = new byte[24 - offset];
            Array.Copy(message, offset, Param, 0, Param.Length);
        }

        public override string ToString()
        {
            return $"(?) Action1. TargetID: {TargetID}, Unk1: {Unknown1}, Unk2: {Unknown2}, Param: {Param.ToHexString()}";
        }
    }

    class FFXIVIpcGuessZoneInto
    {
        public FFXIVLandIdent[] Idents { get; }

        public FFXIVLandIdent Area => Idents[1];
        public FFXIVLandIdent House => Idents[2];

        public string ServerName { get; }

        public FFXIVIpcGuessZoneInto(byte[] message)
        {
            if (message.Length != 64) throw new ArgumentException();
            var offset = 0;

            Idents = new FFXIVLandIdent[4];
            for (int i = 0; i < 4; i++)
            {
                Idents[i].LandId = BitConverter.ToUInt16(message, offset);
                offset += sizeof(UInt16);
                Idents[i].WardNum = BitConverter.ToUInt16(message, offset);
                offset += sizeof(UInt16);
                Idents[i].MapId = BitConverter.ToUInt16(message, offset);
                offset += sizeof(UInt16);
                Idents[i].WorldId = BitConverter.ToUInt16(message, offset);
                offset += sizeof(UInt16);
            }

            int strEnd = 64;
            for (int i = offset; i < 64; i++)
            {
                if (message[i] == 0)
                {
                    strEnd = i;
                    break;
                }
            }

            ServerName = System.Text.UTF8Encoding.UTF8.GetString(message, offset, strEnd - offset - 1);
        }

        public override string ToString()
        {
            return $"(?) Change Zone. Server: {ServerName}, 0: {Idents[0]}, Area: {Idents[1]}, House: {Idents[2]}, 3: {Idents[3]}";
        }
    }

    struct FFXIVLandIdent : IEquatable<FFXIVLandIdent>
    {
        public UInt16 LandId;
        public UInt16 WardNum;
        public UInt16 MapId;
        public UInt16 WorldId;

        public FFXIVLandIdent(UInt16 land, UInt16 ward, UInt16 map, UInt16 world)
        {
            LandId = land;
            WardNum = ward;
            MapId = map;
            WorldId = world;
        }

        public override string ToString()
        {
            return $"({WorldId}, {MapId}, {WardNum}, {LandId})";
        }

        public string ToHexString()
        {
            UInt64 val = (UInt64)LandId + ((UInt64)WardNum << 16) + ((UInt64)MapId << 32) + ((UInt64)WorldId << 48);
            var bytes = BitConverter.GetBytes(val);
            return bytes.ToHexString();
        }

        public bool Equals(FFXIVLandIdent other)
        {
            if (ReferenceEquals(this, other)) return true;

            return LandId == other.LandId && WardNum == other.WardNum && MapId == other.MapId && WorldId == other.WorldId;
        }

        public override bool Equals(object obj) => this.Equals((FFXIVLandIdent)obj);

        public override int GetHashCode()
        {
            return (LandId, WardNum, MapId, WorldId).GetHashCode();
        }

        public static bool operator ==(FFXIVLandIdent a, FFXIVLandIdent b)
        {
            return a.Equals(b);
        }
        public static bool operator !=(FFXIVLandIdent a, FFXIVLandIdent b)
        {
            return !a.Equals(b);
        }
    }

    struct FFXIVItemIndex
    {
        public UInt32 ContainerID;

        public UInt32 SlotID;

        public override string ToString()
        {
            return $"({ContainerID}, {SlotID})";
        }
    }

    struct FFXIVItemIndexer
    {
        public UInt16 ContainerID;

        public UInt16 SlotID;

        public FFXIVItemIndexer(FFXIVItemIndex item)
        {
            ContainerID = (UInt16)item.ContainerID;
            SlotID = (UInt16)item.SlotID;
        }
        public override string ToString()
        {
            return $"({ContainerID}, {SlotID})";
        }
    }

    static class Util
    {
        public static string ToHexString(this byte[] barray)
        {
            char[] c = new char[barray.Length * 2];
            byte b;
            for (int i = 0; i < barray.Length; ++i)
            {
                b = ((byte)(barray[i] >> 4));
                c[i * 2] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                b = ((byte)(barray[i] & 0xF));
                c[i * 2 + 1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }
            return new string(c);
        }
    }
}
