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

            SyncButton.OnExecute += () => {
                this.tracker.WriteStorageToActLog();
            };
        }

        private void TrackerPropertyProxy(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(tracker.CurrentZone))
                OnPropertyChanged(nameof(CurrentZone));
        }

        public string CurrentZone => tracker.GetZoneName(tracker.CurrentZone);

        public string OverlayStatus => "未知";

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
    }

    class SimpleCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public event Action OnExecute;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            OnExecute?.Invoke();
        }
    }
}

