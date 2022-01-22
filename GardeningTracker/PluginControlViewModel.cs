using Lotlab;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
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
    }
}

