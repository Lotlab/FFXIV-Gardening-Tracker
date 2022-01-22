using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
namespace GardeningTracker
{
    /// <summary>
    /// PluginControlWpf.xaml 的交互逻辑
    /// </summary>
    public partial class PluginControl : UserControl
    {
        public PluginControl()
        {
            InitializeComponent();
        }
    }

    public static class AutoScrollBehavior
    {
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(AutoScrollBehavior), new PropertyMetadata(false, AutoScrollPropertyChanged));


        public static void AutoScrollPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var scrollViewer = obj as ScrollViewer;
            if (scrollViewer != null && (bool)args.NewValue)
            {
                scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
                scrollViewer.ScrollToEnd();
            }
            else
            {
                scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
            }
        }

        private static void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Only scroll to bottom when the extent changed. Otherwise you can't scroll up
            if (e.ExtentHeightChange != 0)
            {
                var scrollViewer = sender as ScrollViewer;
                scrollViewer?.ScrollToBottom();
            }
        }

        public static bool GetAutoScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollProperty);
        }

        public static void SetAutoScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollProperty, value);
        }
    }


    [ValueConversion(typeof(Int64), typeof(string))]
    public class UnixTimestampConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                Int64 timestamp = System.Convert.ToInt64(value);
                if (timestamp == 0) return "-";

                return DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return "-";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
