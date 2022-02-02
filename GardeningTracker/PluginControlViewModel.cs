using Lotlab;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

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
                this.tracker.WriteStorageToActLog();
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

        public SimpleCommand SyncButton { get; } = new SimpleCommand();

        public SimpleCommand DeleteCommand { get; } = new SimpleCommand();

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

