using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Data;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace GardeningTracker
{
    class GardeningStorage
    {
        Dictionary<GardeningIdent, GardeningItem> storageDict = new Dictionary<GardeningIdent, GardeningItem>();
        Dictionary<GardeningIdent, GardeningDisplayItem> dispDict = new Dictionary<GardeningIdent, GardeningDisplayItem>();

        public ObservableCollection<GardeningDisplayItem> Gardens { get; } = new ObservableCollection<GardeningDisplayItem>();

        public readonly object StorageLock = new object();

        GardeningData data;
        public GardeningStorage(GardeningData data)
        {
            this.data = data;

            BindingOperations.EnableCollectionSynchronization(Gardens, StorageLock);
        }

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
            lock (StorageLock)
            {
                if (dispDict.ContainsKey(item.Ident))
                    Gardens.Remove(dispDict[item.Ident]);

                storageDict[item.Ident] = item;

                // 创建数据
                data.IsGarden(item.Ident.ObjectID, out var name, out var isPot);
                var houseName = data.GetZoneName(item.Ident.House);
                var landName = data.GetGardenNamePos(item.Ident.ObjectID, item.Ident.LandIndex, item.Ident.LandSubIndex);
                dispDict[item.Ident] = new GardeningDisplayItem(houseName, landName, data.GetSoilName(item.Soil), data.GetSeedName(item.Seed), item.SowTime);

                Gardens.Add(dispDict[item.Ident]);

                // 成熟时间计算
                updateEstMatureTime(item.Ident);
                updateEstWitheredTime(item.Ident);
            }

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
        /// 更新成熟时间
        /// </summary>
        private void updateEstMatureTime(GardeningIdent ident)
        {
            var obj = storageDict[ident];
            var matureTime = data.GetSeedGrowTime(obj.Seed);

            if (matureTime == 0 || obj.SowTime == 0)
            {
                dispDict[ident].EstMatureTime = 0;
                return;
            }
            
            // 计算施肥
            foreach (var item in obj.Fertilizes)
            {
                var remainTime = obj.SowTime + matureTime - item.Time;
                var reduce = remainTime * 0.00989;
                matureTime -= (uint)reduce;
            }

            dispDict[ident].EstMatureTime = obj.SowTime + matureTime;
        }

        /// <summary>
        /// 更新枯萎时间
        /// </summary>
        private void updateEstWitheredTime(GardeningIdent ident)
        {
            var obj = storageDict[ident];
            var wiltTime = data.GetSeedWiltTime(obj.Seed);
            // todo: 不会枯萎的处理
            if (wiltTime == 0)
                dispDict[ident].EstWitheredTime = 0;
            else
                dispDict[ident].EstWitheredTime = obj.LastCare + wiltTime + 24 * 60 * 60;
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

            lock (StorageLock)
            {
                storageDict[ident].Care(time);
                dispDict[ident].LastCare = time;
                // 更新枯萎时间
                updateEstWitheredTime(ident);
            }
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

            lock (StorageLock)
            {
                storageDict[ident].Fertilize(fertilizer, time);
                // 更新成熟时间
                updateEstMatureTime(ident);
            }
        }

        /// <summary>
        /// 处理
        /// </summary>
        /// <param name="ident"></param>
        public void Remove(GardeningIdent ident)
        {
            if (!storageDict.ContainsKey(ident))
                return;

            lock (StorageLock)
            {
                storageDict.Remove(ident);
                Gardens.Remove(dispDict[ident]);
                dispDict.Remove(ident);
            }
        }

        string filePath => Path.Combine(GardeningTracker.DataPath, "garden.json");

        /// <summary>
        /// 加载数据
        /// </summary>
        public void Load()
        {
            if (!File.Exists(filePath))
                return;

            var content = File.ReadAllText(filePath);
            var objs = JsonConvert.DeserializeObject<GardeningItem[]>(content);

            dispDict.Clear();
            storageDict.Clear();
            Gardens.Clear();

            foreach (var item in objs)
            {
                addItem(item);
            }
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        public void Save()
        {
            var rows = storageDict.Values;

            var content = JsonConvert.SerializeObject(rows);
            File.WriteAllText(filePath, content);
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
        public FFXIVLandIdent House { get; set; }

        /// <summary>
        /// 土地物体ID
        /// </summary>
        public uint ObjectID { get; set; }

        /// <summary>
        /// 土地家具位Index
        /// </summary>
        public uint LandIndex { get; set; }

        /// <summary>
        /// 土地家具位子Index
        /// </summary>
        public uint LandSubIndex { get; set; }

        /// <summary>
        /// ActorID
        /// </summary>
        public UInt32 ActorID { get; set; }

        public GardeningIdent() { }

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
        public GardeningIdent Ident { get; set; }

        /// <summary>
        /// 播种土壤
        /// </summary>
        public uint Soil { get; set; }

        /// <summary>
        /// 播种的种子
        /// </summary>
        public uint Seed { get; set; }

        /// <summary>
        /// 播种时间
        /// </summary>
        public UInt64 SowTime { get; set; }

        /// <summary>
        /// 上次护理时间
        /// </summary>
        public UInt64 LastCare { get; set; } = 0;

        /// <summary>
        /// 施肥信息
        /// </summary>
        public List<GardeningFertilizeInfo> Fertilizes { get; set; } = new List<GardeningFertilizeInfo>();

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

        public GardeningItem() { }

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

    class GardeningDisplayItem : PropertyNotifier
    {
        /// <summary>
        /// 所在房屋
        /// </summary>
        public string House { get; }

        /// <summary>
        /// 所在花盆
        /// </summary>
        public string Pot { get; }

        /// <summary>
        /// 播种土壤
        /// </summary>
        public string Soil { get; }

        /// <summary>
        /// 播种的种子
        /// </summary>
        public string Seed { get; }

        /// <summary>
        /// 播种时间
        /// </summary>
        public UInt64 SowTime { get; }

        UInt64 _lastCare = 0;

        /// <summary>
        /// 上次护理时间
        /// </summary>
        public UInt64 LastCare
        {
            get => _lastCare; 
            set
            {
                _lastCare = value;
                OnPropertyChanged();
            }
        }

        UInt64 _estMature = 0;

        /// <summary>
        /// 预计成熟时间
        /// </summary>
        public UInt64 EstMatureTime
        {
            get => _estMature;
            set
            {
                _estMature = value;
                OnPropertyChanged();
            }
        }

        UInt64 _estWithered = 0;

        /// <summary>
        /// 预计枯萎时间
        /// </summary>
        public UInt64 EstWitheredTime
        {
            get => _estWithered;
            set
            {
                _estWithered = value;
                OnPropertyChanged();
            }
        }

        public GardeningDisplayItem(string house, string pot, string soil, string seed, UInt64 sowTime)
        {
            House = house;
            Pot = pot;
            Soil = soil;
            Seed = seed;
            SowTime = sowTime;
            LastCare = sowTime;

            EstMatureTime = 0;
            EstWitheredTime = 0;
        }
    }
}
