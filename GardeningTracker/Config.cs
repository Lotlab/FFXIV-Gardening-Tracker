using Lotlab.PluginCommon;
using System.IO;
using Newtonsoft.Json;

namespace GardeningTracker
{
    public class Config
    {
        public LogLevel LogLevel { get; set; } = LogLevel.INFO;

        public bool AutoSave { get; set; } = true;

        public bool AutoUpdate { get; set; } = true;

        public Config(string fileName)
        {
            configFile = fileName;
        }

        string configFile { get; }

        /// <summary>
        /// 载入配置文件
        /// </summary>
        public void Load()
        {
            if (!File.Exists(configFile)) return;

            var content = File.ReadAllText(configFile);
            var obj = JsonConvert.DeserializeObject<Config>(content);

            LogLevel = obj.LogLevel;
            AutoUpdate = obj.AutoUpdate;
        }

        /// <summary>
        /// 保存配置文件
        /// </summary>
        public void Save()
        {
            var content = JsonConvert.SerializeObject(this);
            File.WriteAllText(configFile, content);
        }
    }
}