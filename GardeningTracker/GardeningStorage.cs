using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GardeningTracker
{
    class GardeningStorage
    {
        Dictionary<GardeningIdent, GardeningItem> storageDict = new Dictionary<GardeningIdent, GardeningItem>();

        public ObservableCollection<GardeningItem> Gardens = new ObservableCollection<GardeningItem>();

        /// <summary>
        /// 播种
        /// </summary>
        public void Sowing(GardeningItem item)
        {
            addItem(item);
        }

        /// <summary>
        /// 添加一个Item
        /// </summary>
        /// <param name="item"></param>
        private void addItem(GardeningItem item)
        {
            storageDict[item.Ident] = item;
            Gardens.Add(storageDict[item.Ident]);
            // todo: notify change
        }

        /// <summary>
        /// 为没播种的添加一个虚假的Item
        /// </summary>
        /// <param name="ident"></param>
        private void createFakeItem(GardeningIdent ident)
        {
            addItem(new GardeningItem(ident, 0, 0, 0));
        }

        /// <summary>
        /// 护理
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="time"></param>
        public void Care(GardeningIdent ident, UInt64 time)
        {
            if (!storageDict.ContainsKey(ident))
                createFakeItem(ident);

            storageDict[ident].Care(time);
        }

        /// <summary>
        /// 施肥
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="fertilizer"></param>
        /// <param name="time"></param>
        public void Fertilize(GardeningIdent ident, uint fertilizer, UInt64 time)
        {
            if (!storageDict.ContainsKey(ident))
                createFakeItem(ident);

            storageDict[ident].Fertilize(fertilizer, time);
        }

        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="ident"></param>
        public void Remove(GardeningIdent ident)
        {
            if (!storageDict.ContainsKey(ident))
                return;

            storageDict.Remove(ident);
        }
    }


    /// <summary>
    /// 土地标志信息
    /// </summary>
    class GardeningIdent : IEquatable<GardeningIdent>
    {
        /// <summary>
        /// 所属房屋信息
        /// </summary>
        public FFXIVLandIdent House { get; }

        /// <summary>
        /// 土地物体ID
        /// </summary>
        public uint ObjectID { get; }

        /// <summary>
        /// 土地家具位Index
        /// </summary>
        public uint LandIndex { get; }

        /// <summary>
        /// 土地家具位子Index
        /// </summary>
        public uint LandSubIndex { get; }

        /// <summary>
        /// ActorID
        /// </summary>
        public UInt32 ActorID { get; }

        public GardeningIdent(FFXIVLandIdent ident, UInt32 actorID, uint landObjId, uint landIndex, uint landSubIndex)
        {
            House = ident;
            ObjectID = landObjId;
            LandIndex = landIndex;
            LandSubIndex = landSubIndex;
            ActorID = actorID;
        }

        public bool Equals(GardeningIdent other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return House == other.House && ActorID == other.ActorID;
        }

        public override bool Equals(object obj) => Equals(obj as GardeningIdent);

        public override int GetHashCode()
        {
            return (House, ActorID).GetHashCode();
        }
    }

    class GardeningItem : PropertyNotifier
    {
        /// <summary>
        /// 所属房屋信息
        /// </summary>
        public GardeningIdent Ident { get; }

        /// <summary>
        /// 播种土壤
        /// </summary>
        public uint Soil { get; }

        /// <summary>
        /// 播种的种子
        /// </summary>
        public uint Seed { get; }

        /// <summary>
        /// 播种时间
        /// </summary>
        public UInt64 SowTime { get; }

        /// <summary>
        /// 上次护理时间
        /// </summary>
        public UInt64 LastCare { get; private set; } = 0;

        /// <summary>
        /// 施肥信息
        /// </summary>
        public List<GardeningFertilizeInfo> Fertilizes { get; } = new List<GardeningFertilizeInfo>();

        public GardeningItem(FFXIVLandIdent ident, UInt32 actorID, uint landObjId, uint landIndex, uint landSubIndex, uint soil, uint seed, UInt64 time) :
            this(new GardeningIdent(ident, actorID, landObjId, landIndex, landSubIndex), soil, seed, time)
        {
        }

        public GardeningItem(GardeningIdent ident, uint soil, uint seed, UInt64 time)
        {
            Ident = ident;
            Soil = soil;
            Seed = seed;
            SowTime = time;
            LastCare = SowTime;
        }

        /// <summary>
        /// 护理植物
        /// </summary>
        /// <param name="time"></param>
        public void Care(UInt64 time)
        {
            LastCare = time;
            OnPropertyChanged(nameof(LastCare));
        }

        /// <summary>
        /// 施肥
        /// </summary>
        /// <param name="fertilizer"></param>
        /// <param name="time"></param>
        public void Fertilize(uint fertilizer, UInt64 time)
        {
            Fertilizes.Add(new GardeningFertilizeInfo() { Time = time, Type = fertilizer });
            // todo: property change
        }
    }

    /// <summary>
    /// 施肥信息
    /// </summary>
    struct GardeningFertilizeInfo
    {
        /// <summary>
        /// 肥料类型（ID）
        /// </summary>
        public uint Type;

        /// <summary>
        /// 施肥时间
        /// </summary>
        public UInt64 Time;
    }


}
