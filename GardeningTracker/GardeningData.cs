using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace GardeningTracker
{
    struct FFXIVItem
    {
        public uint Id;
        public string Name;
    }

    struct FFXIVSeedInfo
    {
        public uint Index;
        public FFXIVItem Seed;
        public FFXIVItem Item;
    }

    struct FFXIVSeedTimeInfo
    {
        public uint Index;
        public uint GrowTime;
        public uint WiltTime;
    }

    struct SeedTime
    {
        public uint GrowTime;
        public uint WiltTime;
    }

    /// <summary>
    /// 园艺数据
    /// </summary>
    class GardeningData
    {
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetAssembly(typeof(GardeningData)).CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        T readJsonDataFile<T>(string name)
        {
            var plugin = PluginGardeningTracker.ActPlugin;
            var dir = plugin != null ? Path.GetDirectoryName(plugin.pluginFile.FullName) : AssemblyDirectory;

            var filePath = Path.Combine(dir, "data", name);
            var content = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(content);
        }

        /// <summary>
        /// 土壤ID名称对应表
        /// </summary>
        public Dictionary<uint, string> SoilNames = new Dictionary<uint, string>();

        /// <summary>
        /// 肥料ID名称对应表
        /// </summary>
        public Dictionary<uint, string> FertilizerNames = new Dictionary<uint, string>();

        /// <summary>
        /// 种子名称表
        /// </summary>
        public Dictionary<uint, string> SeedNames = new Dictionary<uint, string>();

        /// <summary>
        /// 产物名称表
        /// </summary>
        public Dictionary<uint, string> ProductNames = new Dictionary<uint, string>();

        /// <summary>
        /// 种子产物对应表
        /// </summary>
        public Dictionary<uint, uint> SeedProducts = new Dictionary<uint, uint>();

        /// <summary>
        /// 种子时间信息
        /// </summary>
        public Dictionary<uint, SeedTime> SeedTimeInfos = new Dictionary<uint, SeedTime>();

        /// <summary>
        /// 花盆名称表
        /// </summary>
        public Dictionary<uint, string> PotNameDict = new Dictionary<uint, string>()
        {
            { 197051, "海滨花盆" },
            { 197052, "林间花盆" },
            { 197053, "绿洲花盆" },
        };

        /// <summary>
        /// 园圃名称表
        /// </summary>
        public Dictionary<uint, string> GardenRidgeNameDict = new Dictionary<uint, string>()
        {
            { 2003757, "园圃" },
            // 以下的应该是未用的，不知道为啥在这里
            { 2008701, "苗圃" },
            { 2008702, "苗圃" },
            { 2008703, "苗圃" },
            { 2008704, "苗圃" },
            { 2008705, "苗圃" },
            { 2008706, "苗圃" },
            { 2008707, "苗圃" },
            { 2008708, "苗圃" },
        };

        /// <summary>
        /// 判断是否为可种植区域，并获取名字和类型
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="name">名称</param>
        /// <param name="isPot">类型</param>
        /// <returns></returns>
        public bool IsGarden(uint id, out string name, out bool isPot)
        {
            if (PotNameDict.ContainsKey(id) || GardenRidgeNameDict.ContainsKey(id))
            {
                isPot = PotNameDict.ContainsKey(id);
                name = isPot ? PotNameDict[id] : GardenRidgeNameDict[id];
                return true;
            }

            isPot = false;
            name = null;
            return false;
        }

        /// <summary>
        /// 房区名称
        /// </summary>
        public Dictionary<uint, string> MapNames = new Dictionary<uint, string>()
        {
            { 0x153, "海雾村" },
            { 0x154, "薰衣草苗圃" },
            { 0x155, "高脚孤丘" },
            { 0x281, "白银乡" },
            { 0x3D3, "穹顶皓天" },
        };

        /// <summary>
        /// 公寓名称
        /// </summary>
        public Dictionary<uint, string> DepartmentNames = new Dictionary<uint, string>()
        {
            { 0x153, "中桅塔" },
            { 0x154, "百合岭" },
            { 0x155, "娜娜莫大风车" },
            { 0x281, "红梅御殿" },
            { 0x3D3, "公寓" },
        };

        /// <summary>
        /// 读取外部数据
        /// </summary>
        public void Read()
        {
            // 土壤信息
            var soils = readJsonDataFile<FFXIVItem[]>("soils.json");
            SoilNames.Clear();
            foreach (var item in soils)
                SoilNames[item.Id] = item.Name;

            // 肥料信息
            var fertilizers = readJsonDataFile<FFXIVItem[]>("fertilizers.json");
            FertilizerNames.Clear();
            foreach (var item in fertilizers)
                FertilizerNames[item.Id] = item.Name;

            // 种子信息
            var seedInfos = readJsonDataFile<FFXIVSeedInfo[]>("seeds.json");
            SeedNames.Clear();
            ProductNames.Clear();
            SeedProducts.Clear();
            Dictionary<uint, uint> seedIndexDict = new Dictionary<uint, uint>();
            foreach (var item in seedInfos)
            {
                SeedNames[item.Seed.Id] = item.Seed.Name;
                ProductNames[item.Item.Id] = item.Item.Name;
                SeedProducts[item.Seed.Id] = item.Item.Id;
                seedIndexDict[item.Index] = item.Seed.Id;
            }

            // 种子时间信息
            var seedExtInfos = readJsonDataFile<FFXIVSeedTimeInfo[]>("seeds_time.json");
            SeedTimeInfos.Clear();
            foreach (var item in seedExtInfos)
            {
                var seedId = seedIndexDict[item.Index];
                SeedTimeInfos[seedId] = new SeedTime() { GrowTime = item.GrowTime, WiltTime = item.WiltTime };
            }
        }

        /// <summary>
        /// 获取区域名称
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="indoor">是否解析室内信息</param>
        /// <returns></returns>
        public string GetZoneName(FFXIVLandIdent ident, bool indoor = true)
        {
            if (!MapNames.ContainsKey(ident.MapId))
                return "未知区域";

            var wardNum = ident.WardNum & 0x3F;
            var str = $"{MapNames[ident.MapId]} 第{wardNum}区";

            if (indoor)
            {
                if (ident.LandId < 0x80)
                {
                    str += $" {ident.LandId + 1}号房";
                }
                else
                {
                    str += $" {DepartmentNames[ident.MapId]}{ident.LandId - 0x7F}号楼";
                }

                var roomNum = ident.WardNum >> 6;
                if (roomNum != 0)
                {
                    str += $" {roomNum}号房间";
                }
                else if (ident.LandId >= 0x80)
                {
                    str += $" 大厅";
                }
            }

            return str;
        }
    
        /// <summary>
        /// 获取种子名称
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetSeedName(uint id)
        {
            if (SeedNames.ContainsKey(id)) return SeedNames[id];

            return $"未知种子({id})";
        }

        /// <summary>
        /// 获取土壤名称
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetSoilName(uint id)
        {
            if (SoilNames.ContainsKey(id)) return SoilNames[id];

            return $"未知土壤({id})";
        }

        /// <summary>
        /// 获取花盆位置名称
        /// </summary>
        /// <param name="objID"></param>
        /// <param name="index"></param>
        /// <param name="subIndex"></param>
        /// <returns></returns>
        public string GetGardenNamePos(uint objID, uint index, uint subIndex)
        {
            bool valid = IsGarden(objID, out var potName, out var isPot);
            if (!valid) potName = "未知";
            var potPos = $"{index + 1}";
            if (!isPot) potPos += $",{subIndex + 1}";

            return $"{potName}({potPos})";
        }

        /// <summary>
        /// 获取种子生长时间
        /// </summary>
        /// <param name="seedId"></param>
        /// <returns></returns>
        public uint GetSeedGrowTime(uint seedId)
        {
            if (!SeedTimeInfos.ContainsKey(seedId))
                return 0;
            
            return SeedTimeInfos[seedId].GrowTime * 60 * 60;
        }

        /// <summary>
        /// 获取种子冒烟时间
        /// </summary>
        /// <param name="seedId"></param>
        /// <returns></returns>
        public uint GetSeedWiltTime(uint seedId)
        {
            if (!SeedTimeInfos.ContainsKey(seedId))
                return 0;

            return SeedTimeInfos[seedId].WiltTime * 60 * 60;
        }
    }
}

