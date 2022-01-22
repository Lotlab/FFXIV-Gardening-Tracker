using System;
using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Common;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Lotlab;
using System.IO;

namespace GardeningTracker
{
    public class GardeningTracker : IActPluginV1
    {
        IDataSubscription _ffxivPlugin = null;

        IDataSubscription ffxivPlugin
        {
            get
            {
                if (_ffxivPlugin == null)
                {
                    var plugins = ActGlobals.oFormActMain.ActPlugins;
                    foreach (var item in plugins)
                    {
                        if (item.pluginFile.Name.ToUpper().Contains("FFXIV_ACT_PLUGIN"))
                        {
                            _ffxivPlugin = item.pluginObj.GetType().GetProperty("DataSubscription").GetValue(item.pluginObj) as IDataSubscription;
                        }
                    }
                }
                return _ffxivPlugin;
            }
        }

        Label lblStatus;

        PluginControlViewModel viewModel;
        Config config;
        GardeningTrackerInner tracker;
        SimpleLogger logger;

        public static GardeningTracker Instance = null;
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

            if (ffxivPlugin == null)
            {
                lblStatus.Text = "FFXIV Act Plugin is not loading.";
                return;
            }

            pluginInit();
            guiInit(pluginScreenSpace);
        }

        void guiInit(TabPage page)
        {
            page.Text = "园艺时钟";

            var control = new PluginControl();
            control.DataContext = viewModel;
            var host = new ElementHost()
            {
                Dock = DockStyle.Fill,
                Child = control
            };

            page.Controls.Add(host);
        }

        string DataPath => Path.Combine(Environment.CurrentDirectory, "AppData", "GardeningTracker");

        void prepareDir()
        {
            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);
        }

        void pluginInit()
        {
            prepareDir();

            config = new Config();
            config.LoadSettings();

            logger = new SimpleLogger(Path.Combine(DataPath, "app.log"), LogLevel.TRACE);
            viewModel = new PluginControlViewModel(config, logger);
            tracker = new GardeningTrackerInner(viewModel, config, logger, actLog);

            // Register events
            ffxivPlugin.NetworkSent += onNetworkSend;
            ffxivPlugin.NetworkReceived += onNetworkReceive;

            logger.LogInfo($"初始化完毕");
        }

        void pluginDeinit()
        {
            ffxivPlugin.NetworkSent -= onNetworkSend;
            ffxivPlugin.NetworkReceived -= onNetworkReceive;

            logger.Close();
            logger = null;
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
