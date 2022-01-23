using Lotlab;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

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
        }

        private void TrackerPropertyProxy(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(tracker.CurrentZone))
                OnPropertyChanged(nameof(CurrentZone));
        }

        public string CurrentZone => tracker.GetZoneName(tracker.CurrentZone);

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
    }
}

