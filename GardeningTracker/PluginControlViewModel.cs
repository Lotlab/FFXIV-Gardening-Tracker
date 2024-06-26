﻿using Lotlab.PluginCommon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows;

namespace GardeningTracker
{
    class PluginControlViewModel : PropertyNotifier
    {
        public ObservableCollection<LogItem> Logs => tracker.Logger.ObserveLogs;
        public ObservableCollection<GardeningDisplayItem> Gardens => tracker.Storage.Gardens;

        GardeningTracker tracker;

        public PluginControlViewModel(GardeningTracker tracker)
        {
            this.tracker = tracker;

            tracker.PropertyChanged += TrackerPropertyProxy;

            SyncButton.OnExecute += (obj) =>
            {
                this.tracker.SyncContent();
            };
            DeleteCommand.OnExecute += (obj) =>
            {
                if (SelectedItem != null)
                {
                    this.tracker.Logger.LogInfo($"删除 {SelectedItem.House} {SelectedItem.Pot}");
                    this.tracker.Storage.Remove(SelectedItem.Ident);
                    SelectedItem = null;
                }
            };
            CheckUpdateButton.OnExecute += (obj) =>
            {
                this.tracker.CheckUpdate();
            };

            OpcodeGuideStart.OnExecute += (obj) =>
            {
                this.tracker.opcodeGuide.Restart();
            };
            OpcodeGuideNext.OnExecute += (obj) =>
            {
                this.tracker.opcodeGuide.Skip();
            };
            OpcodeGuideSave.OnExecute += (obj) =>
            {
                this.tracker.opcodeGuide.Save();
            };
            HybridStatsTest.OnExecute += (obj) =>
            {
                this.tracker.hybridStats.UploadTestData();
            };
        }

        private void TrackerPropertyProxy(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(tracker.CurrentZone):
                    OnPropertyChanged(nameof(CurrentZone));
                    break;
                case nameof(tracker.OverlayInited):
                    OnPropertyChanged(nameof(OverlayStatus));
                    break;
                default:
                    break;
            }
        }

        public string CurrentZone => tracker.GetZoneName(tracker.CurrentZone);

        public string OverlayStatus => tracker.OverlayInited ? "已连接" : "未连接";

        public LogLevel LogLevel
        {
            get { return tracker.Config.LogLevel; }
            set
            {
                tracker.Config.LogLevel = value;
                tracker.Logger.SetFilter(value);
                OnPropertyChanged();
            }
        }

        public IEnumerable<LogLevel> LogLevels
        {
            get
            {
                return Enum.GetValues(typeof(LogLevel))
                    .Cast<LogLevel>();
            }
        }

        public bool AutoSave
        {
            get => tracker.Config.AutoSave;
            set
            {
                tracker.Config.AutoSave = value;
                OnPropertyChanged();
            }
        }

        public bool AutoUpdate
        {
            get => tracker.Config.AutoUpdate;
            set
            {
                tracker.Config.AutoUpdate = value;
                OnPropertyChanged();
            }
        }

        public string StatsWebhookUrl
        {
            get => tracker.Config.StatsWebhookUrl;
            set
            {
                tracker.Config.StatsWebhookUrl = value;
                OnPropertyChanged();
            }
        }

        public string StatsWebhookToken
        {
            get => tracker.Config.StatsWebhookToken;
            set
            {
                tracker.Config.StatsWebhookToken = value;
                OnPropertyChanged();
            }
        }

        public string StatsUserName
        {
            get => tracker.Config.StatsUserName;
            set
            {
                tracker.Config.StatsUserName = value;
                OnPropertyChanged();
            }
        }

        bool debug = false;
        public bool Debug {
            get => debug;
            set
            {
                debug = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DebugVisible));
            }
        }

        public Visibility DebugVisible => Debug ? Visibility.Visible : Visibility.Hidden;

        public SimpleCommand SyncButton { get; } = new SimpleCommand();

        public SimpleCommand DeleteCommand { get; } = new SimpleCommand();

        public SimpleCommand CheckUpdateButton { get; } = new SimpleCommand();
        public SimpleCommand OpcodeGuideStart { get; } = new SimpleCommand();
        public SimpleCommand OpcodeGuideNext { get; } = new SimpleCommand();
        public SimpleCommand OpcodeGuideSave { get; } = new SimpleCommand();
        public SimpleCommand HybridStatsTest { get; } = new SimpleCommand();

        private GardeningDisplayItem _selectedItem = null;
        public GardeningDisplayItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
            }
        }
    }

    class SimpleCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public event Action<object> OnExecute;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            OnExecute?.Invoke(parameter);
        }
    }
}

