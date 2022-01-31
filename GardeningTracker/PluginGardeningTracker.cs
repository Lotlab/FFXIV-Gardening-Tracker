using System;
using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Common;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Text.RegularExpressions;
using System.Linq;

namespace GardeningTracker
{
    public partial class PluginGardeningTracker : IActPluginV1
    {
        IDataSubscription _ffxivDataSub = null;

        IDataRepository _ffxivDataRepo = null;

        IDataSubscription ffxivDataSub
        {
            get
            {
                if (_ffxivDataSub == null)
                    getXIVPlugin();

                return _ffxivDataSub;
            }
        }

        IDataRepository ffxivDataRepo
        {
            get
            {
                if (_ffxivDataRepo == null)
                    getXIVPlugin();

                return _ffxivDataRepo;
            }
        }
        private void getXIVPlugin()
        {
            var plugins = ActGlobals.oFormActMain.ActPlugins;
            foreach (var item in plugins)
            {
                if (item.pluginFile.Name.ToUpper().Contains("FFXIV_ACT_PLUGIN"))
                {
                    _ffxivDataSub = item.pluginObj.GetType().GetProperty("DataSubscription").GetValue(item.pluginObj) as IDataSubscription;
                    _ffxivDataRepo = item.pluginObj.GetType().GetProperty("DataRepository").GetValue(item.pluginObj) as IDataRepository;
                }
            }
        }

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

            if (ffxivDataSub == null)
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
            ffxivDataSub.NetworkSent += onNetworkSend;
            ffxivDataSub.NetworkReceived += onNetworkReceive;
            ffxivDataSub.LogLine += onLogLine;

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

            var worldID = ffxivDataRepo.GetCombatantList()
                .FirstOrDefault(x => x.ID == ffxivDataRepo.GetCurrentPlayerID()).CurrentWorldID;

            tracker.SystemLogZoneChange(worldID, map, wardNum);
        }

        void pluginDeinit()
        {
            ffxivDataSub.NetworkSent -= onNetworkSend;
            ffxivDataSub.NetworkReceived -= onNetworkReceive;
            ffxivDataSub.LogLine -= onLogLine;

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
