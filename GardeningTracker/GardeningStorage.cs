﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Data;
using System.Windows.Threading;
using Newtonsoft.Json;
using Lotlab.PluginCommon;
using Lotlab.PluginCommon.FFXIV.Parser.Packets;
using Lotlab.PluginCommon.FFXIV.Parser;

namespace GardeningTracker
{
    class GardeningStorage
    {
        Dictionary<GardeningIdent, GardeningItem> storageDict = new Dictionary<GardeningIdent, GardeningItem>();
        Dictionary<GardeningIdent, GardeningDisplayItem> dispDict = new Dictionary<GardeningIdent, GardeningDisplayItem>();

        public ObservableCollection<GardeningDisplayItem> Gardens { get; set; } = new ObservableCollection<GardeningDisplayItem>();

        public event Action OnDataChange;

        public readonly object StorageLock = new object();

        GardeningData data { get; }
        Config config { get; }
        SimpleLogger logger { get; }
        public GardeningStorage(SimpleLogger logger, GardeningData data, Config config, string saveFilePath)
        {
            this.data = data;
            this.config = config;
            this.logger = logger;
            filePath = saveFilePath;
            BindingOperations.EnableCollectionSynchronization(Gardens, StorageLock);
        }

        /// <summary>
        /// 播种
        /// </summary>
        public void Sowing(GardeningItem item)
        {
            if (item.SowTime == 0)
                item.SowTime = (ulong)DateTimeOffset.Now.ToUnixTimeSeconds();

            addItem(item);
            notifyDataChange();
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
                dispDict[item.Ident] = new GardeningDisplayItem(item.Ident, houseName, landName, data.GetSoilName(item.Soil), data.GetSeedName(item.Seed), item.SowTime, item.LastCare);
                dispDict[item.Ident].PropertyChanged += onPropChanged;

                Gardens.Add(dispDict[item.Ident]);

                // 成熟时间计算
                updateEstMatureTime(item.Ident);
                updateEstWitheredTime(item.Ident);
                updateEstColor(item.Ident);
            }

        }

        private void onPropChanged(object sender, PropertyChangedEventArgs e)
        {
            GardeningDisplayItem item = sender as GardeningDisplayItem;
            if (item == null) return;

            switch(e.PropertyName)
            {
                case nameof(GardeningDisplayItem.LastCare):
                    lock (StorageLock)
                    {
                        storageDict[item.Ident].Care(item.LastCare);
                        updateEstWitheredTime(item.Ident);
                    }
                    break;
                case nameof(GardeningDisplayItem.SowTime):
                    lock (StorageLock)
                    {
                        storageDict[item.Ident].SowTime = item.SowTime;
                        updateEstMatureTime(item.Ident);
                    }
                    break;
            }
        }

        /// <summary>
        /// 为没播种的添加一个虚假的Item
        /// </summary>
        /// <param name="ident"></param>
        private void createFakeItem(GardeningIdent ident, uint seed = 0)
        {
            addItem(new GardeningItem(ident, 0, seed, 0));
        }

        /// <summary>
        /// 更新估计的颜色
        /// </summary>
        /// <param name="ident"></param>
        private void updateEstColor(GardeningIdent ident)
        {
            var obj = storageDict[ident];
            var color = "";
            foreach (var item in obj.Fertilizes)
            {
                var name = data.GetFertilizerColor(item.Type);
                if (name != "")
                {
                    if (color != "") color += ", ";
                    color += name;
                }
            }
            dispDict[ident].Color = color;
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
                if (remainTime > 0)
                {
                    var reduce = remainTime * 0.00989;
                    matureTime -= (uint)reduce;
                }
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

            if (wiltTime == 0 || obj.LastCare == 0)
                dispDict[ident].EstWitheredTime = 0;
            else
                dispDict[ident].EstWitheredTime = obj.LastCare + wiltTime + 24 * 60 * 60;
        }

        /// <summary>
        /// 护理
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="time"></param>
        public void Care(GardeningIdent ident, UInt64 time, uint guessSeed = 0)
        {
            if (!storageDict.ContainsKey(ident))
                createFakeItem(ident, guessSeed);

            if (time == 0)
                time = (ulong)DateTimeOffset.Now.ToUnixTimeSeconds();

            lock (StorageLock)
            {
                // 如果之前没获取到正确的种子，那么在这里获取
                if (storageDict[ident].Seed == 0 && guessSeed != 0)
                {
                    storageDict[ident].Seed = guessSeed;
                    dispDict[ident].Seed = data.GetSeedName(guessSeed);
                }

                storageDict[ident].Care(time);
                dispDict[ident].CareWithoutNotify(time);

                // 更新枯萎时间
                updateEstWitheredTime(ident);
                updateEstColor(ident);
            }

            notifyDataChange();
        }

        /// <summary>
        /// 施肥
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="fertilizer"></param>
        /// <param name="time"></param>
        public void Fertilize(GardeningIdent ident, uint fertilizer, UInt64 time, uint guessSeed = 0)
        {
            if (!storageDict.ContainsKey(ident))
                createFakeItem(ident, guessSeed);

            if (time == 0)
                time = (ulong)DateTimeOffset.Now.ToUnixTimeSeconds();

            lock (StorageLock)
            {
                storageDict[ident].Fertilize(fertilizer, time);
                // 更新成熟时间
                updateEstMatureTime(ident);
            }

            notifyDataChange();
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

            notifyDataChange();
        }

        string filePath { get; }
        string backupFilePath => filePath + ".bak";

        /// <summary>
        /// 加载数据
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    loadFileAt(filePath);
                    return;
                }
                else
                {
                    throw new FileNotFoundException();
                }
            }
            catch
            {
                if (File.Exists(backupFilePath))
                {
                    loadFileAt(backupFilePath);
                }
            }
        }

        /// <summary>
        /// 加载位于指定位置的存储数据文件
        /// </summary>
        /// <param name="path"></param>
        private void loadFileAt(string path)
        {
            var content = File.ReadAllText(path);
            var objs = JsonConvert.DeserializeObject<GardeningItem[]>(content);

            lock (StorageLock)
            {
                dispDict.Clear();
                storageDict.Clear();
                Gardens.Clear();

                foreach (var item in objs)
                {
                    addItem(item);
                }
            }
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        public void Save()
        {
            string content = GetJson();

            if (File.Exists(filePath))
            {
                if (File.Exists(backupFilePath))
                    File.Delete(backupFilePath);

                File.Move(filePath, backupFilePath);
            }

            File.WriteAllText(filePath, content);
        }

        public IEnumerable<GardeningItem> GetStorageItems()
        {
            return storageDict.Values;
        }

        /// <summary>
        /// 读取存储数据的Json
        /// </summary>
        /// <returns></returns>
        public string GetJson()
        {
            var content = JsonConvert.SerializeObject(GetStorageItems());
            return content;
        }

        void notifyDataChange()
        {
            OnDataChange?.Invoke();
            AutoSave();
        }

        /// <summary>
        /// 判断自动保存并保存
        /// </summary>
        public void AutoSave()
        {
            if (config.AutoSave)
            {
                Save();
            }
        }
    }

    struct FFXIVLandIdent : IEquatable<FFXIVLandIdent>
    {
        public UInt16 LandId;
        public UInt16 WardNum;
        public UInt16 MapId;
        public UInt16 WorldId;

        public FFXIVLandIdent(LandIdent ident)
        {
            LandId = ident.landId;
            WardNum = ident.wardNum;
            MapId = ident.territoryTypeId;
            WorldId = ident.worldId;
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

        public uint HousingLink { get; set; }

        public GardeningIdent() { }

        public GardeningIdent(FFXIVLandIdent ident, uint landObjId, UInt32 housingLink, bool isIndoor)
        {
            House = ident;
            ObjectID = landObjId;
            HousingLink = housingLink;

            LandIndex = (uint)(HousingLink & (isIndoor ? 0xFFFF : 0x00FF));
            LandSubIndex = (HousingLink >> 24) & 0xFF;
        }

        public bool Equals(GardeningIdent other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return House.Equals(other.House) && ObjectID == other.ObjectID && LandIndex == other.LandIndex && LandSubIndex == other.LandSubIndex;
        }

        public override bool Equals(object obj) => Equals(obj as GardeningIdent);

        public override int GetHashCode()
        {
            return (House, ObjectID, LandIndex, LandSubIndex).GetHashCode();
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
        public UInt64 LastCare { get; set; }

        /// <summary>
        /// 施肥信息
        /// </summary>
        public List<GardeningFertilizeInfo> Fertilizes { get; set; } = new List<GardeningFertilizeInfo>();

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
        public GardeningIdent Ident { get; }

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
        public string Seed { get; set; }

        /// <summary>
        /// 肥料颜色
        /// </summary>
        public string Color { get; set; }

        UInt64 _sowTime;

        /// <summary>
        /// 播种时间
        /// </summary>
        public UInt64 SowTime 
        {
            get => _sowTime;
            set
            {
                _sowTime = value;
                OnPropertyChanged();
            }
        }

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

        public void CareWithoutNotify(UInt64 value)
        {
            _lastCare = value;
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

        public GardeningDisplayItem(GardeningIdent ident, string house, string pot, string soil, string seed, UInt64 sowTime, UInt64 lastCare)
        {
            Ident = ident;
            House = house;
            Pot = pot;
            Soil = soil;
            Seed = seed;
            SowTime = sowTime;
            LastCare = lastCare;

            EstMatureTime = 0;
            EstWitheredTime = 0;
            Color = "";
        }
    }
}
