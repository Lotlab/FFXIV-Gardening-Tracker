using System;
using Advanced_Combat_Tracker;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Text.RegularExpressions;
using System.Linq;
using Lotlab.PluginCommon.FFXIV;
using System.IO;

namespace GardeningTracker
{
    public partial class PluginGardeningTracker : IActPluginV1
    {

        ACTPluginProxy ffxiv;

        Label lblStatus;

        GardeningTracker tracker;

        public static PluginGardeningTracker Instance = null;
        public static ActPluginData ActPlugin
        {
            get
            {
                if (Instance == null) return null;

                var plugins = ActGlobals.oFormActMain.ActPlugins;
                foreach (var item in plugins)
                {
                    if (item.pluginObj == Instance)
                    {
                        return item;
                    }
                }
                return null;
            }
        }

        void IActPluginV1.DeInitPlugin()
        {
            lblStatus.Text = "Plugin exit.";
            pluginDeinit();
        }

        void IActPluginV1.InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            Instance = this;

            lblStatus = pluginStatusText;
            lblStatus.Text = "Plugin working.";

            var plugins = ActGlobals.oFormActMain.ActPlugins;
            foreach (var item in plugins)
            {
                if (item.pluginFile.Name.ToUpper().Contains("FFXIV_ACT_PLUGIN") && item.pluginObj != null)
                {
                    ffxiv = new ACTPluginProxy(item.pluginObj);
                }
            }

            if (ffxiv == null || !ffxiv.PluginStarted)
            {
                lblStatus.Text = "FFXIV Act Plugin is not loading.";
                return;
            }

            pluginInit(pluginScreenSpace);
        }

        void pluginInit(TabPage page)
        {
            var asmDir = Path.Combine(ActPlugin.pluginFile.DirectoryName, "data");
            var appDataDir = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.Parent.FullName, "GardeningTracker");
            prepareDir(appDataDir);

            tracker = new GardeningTracker(actLog, asmDir, appDataDir);

            // Register events
            ffxiv.DataSubscription.NetworkSent += onNetworkSend;
            ffxiv.DataSubscription.NetworkReceived += onNetworkReceive;
            ffxiv.DataSubscription.LogLine += onLogLine;

            page.Text = "园艺时钟";

            var control = new PluginControl();
            control.DataContext = new PluginControlViewModel(tracker);
            var host = new ElementHost()
            {
                Dock = DockStyle.Fill,
                Child = control
            };

            page.Controls.Add(host);
        }

        private void onLogLine(uint EventType, uint Seconds, string logline)
        {
            if (EventType != 57) return;

            var match = Regex.Match(logline, "(.+)第([0-9]+)区");
            if (!match.Success) return;

            var map = match.Groups[1].Value;
            var ward = match.Groups[2].Value;

            if (!int.TryParse(ward, out int wardNum))
            {
                tracker.Logger.LogDebug($"区域解析失败。{map}, {ward}");
                return;
            }

            tracker.SystemLogZoneChange(GetWorldID(), map, wardNum);
        }

        uint GetWorldID()
        {
            var list = ffxiv.DataRepository.GetCombatantList();
            var currentID = ffxiv.DataRepository.GetCurrentPlayerID();
            foreach (var item in list)
            {
                if (item.ID == currentID)
                    return item.CurrentWorldID;
            }

            return 0;
        }

        void pluginDeinit()
        {
            if (ffxiv != null)
            {
                ffxiv.DataSubscription.NetworkSent -= onNetworkSend;
                ffxiv.DataSubscription.NetworkReceived -= onNetworkReceive;
                ffxiv.DataSubscription.LogLine -= onLogLine;
                ffxiv = null;
            }

            tracker?.DeInit();
        }

        private void onNetworkSend(string connection, long epoch, byte[] message)
        {
            tracker?.NetworkSend(message);
        }

        private void onNetworkReceive(string connection, long epoch, byte[] message)
        {
            tracker?.NetworkReceive(message);
        }

        private void actLog(string str)
        {
            ActGlobals.oFormActMain.ParseRawLogLine(false, DateTime.Now, str);
        }

        void prepareDir(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
