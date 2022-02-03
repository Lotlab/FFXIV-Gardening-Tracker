using System;
using Advanced_Combat_Tracker;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Text.RegularExpressions;
using System.Linq;

namespace GardeningTracker
{
    public partial class PluginGardeningTracker : IActPluginV1
    {

        FFXIVPluginProxy ffxiv = new FFXIVPluginProxy();

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

            ffxiv.InitPlugin();

            if (!ffxiv.Inited)
            {
                lblStatus.Text = "FFXIV Act Plugin is not loading.";
                return;
            }

            pluginInit(pluginScreenSpace);
        }

        void pluginInit(TabPage page)
        {
            tracker = new GardeningTracker(actLog);

            // Register events
            ffxiv.NetworkSent += onNetworkSend;
            ffxiv.NetworkReceived += onNetworkReceive;
            ffxiv.LogLine += onLogLine;

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

            var worldID = ffxiv.GetWorldID();

            tracker.SystemLogZoneChange(worldID, map, wardNum);
        }

        void pluginDeinit()
        {
            ffxiv.NetworkSent -= onNetworkSend;
            ffxiv.NetworkReceived -= onNetworkReceive;
            ffxiv.LogLine -= onLogLine;
            ffxiv.DeinitPlugin();

            tracker.DeInit();
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
    }
}
