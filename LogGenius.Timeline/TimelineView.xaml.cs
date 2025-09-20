using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace LogGenius.Modules.Timeline
{
    public class GetTimelineViewScrollBarMaximumValue : IMultiValueConverter
    {
        public object? Convert(object[] Values, Type TargetType, object Parameter, CultureInfo Culture)
        {
            if (Values.Length < 2)
            {
                return null;
            }
            var Timeline = Values[0] as Timeline;
            if (Timeline == null)
            {
                return (double)0;
            }
            if (Values[1].GetType() != typeof(double))
            {
                return null;
            }
            var ActualWidth = (double)Values[1];
            if (Values[2].GetType() != typeof(double))
            {
                return null;
            }
            var HeaderWidth = (double)Values[2];
            return Math.Max(0, Timeline!.TotalMilliseconds / Timeline!.MillisecondPerPixel - (ActualWidth - HeaderWidth));
        }

        public object[] ConvertBack(object Value, Type[] TargetTypes, object Parameter, CultureInfo Culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class TimelineView : UserControl
    {
        public static readonly DependencyProperty TimelineProperty =
            DependencyProperty.Register(
                nameof(Timeline),
                typeof(Timeline),
                typeof(TimelineView),
                new PropertyMetadata(null, OnTimelineChanged));

        public Timeline? Timeline
        {
            get => (Timeline)GetValue(TimelineProperty);
            set => SetValue(TimelineProperty, value);
        }

        private static void OnTimelineChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
        }

        public static readonly DependencyProperty HeaderWidthProperty =
            DependencyProperty.Register(
                nameof(HeaderWidth),
                typeof(double),
                typeof(TimelineView),
                new PropertyMetadata(100.0, OnHeaderWidthChanged));

        public double HeaderWidth
        {
            get => (double)GetValue(HeaderWidthProperty);
            set => SetValue(HeaderWidthProperty, value);
        }

        private static void OnHeaderWidthChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
        }

        public static readonly DependencyProperty MinHeaderWidthProperty =
            DependencyProperty.Register(
                nameof(MinHeaderWidth),
                typeof(double),
                typeof(TimelineView),
                new PropertyMetadata(100.0));

        public double MinHeaderWidth
        {
            get => (double)GetValue(MinHeaderWidthProperty);
            set => SetValue(MinHeaderWidthProperty, value);
        }

        public static readonly DependencyProperty MaxHeaderWidthProperty =
            DependencyProperty.Register(
                nameof(MaxHeaderWidth),
                typeof(double),
                typeof(TimelineView),
                new PropertyMetadata(300.0));

        public double MaxHeaderWidth
        {
            get => (double)GetValue(MaxHeaderWidthProperty);
            set => SetValue(MaxHeaderWidthProperty, value);
        }

        public TimelineView()
        {
            InitializeComponent();
        }

        private void OnScrollBarValueChanged(object Sender, RoutedPropertyChangedEventArgs<double> EventArgs)
        {
            if (this.Timeline == null)
            {
                return;
            }
            foreach (var Section in this.Timeline.Sections)
            {
                if (PART_ListView.ItemContainerGenerator.ContainerFromItem(Section) is ListViewItem ListViewItem)
                {
                    var SectionView = ModernWpf.VisualTree.FindDescendant<SectionView>(ListViewItem);
                    SectionView.Invalidate();
                }
            }
        }

        private void OnListViewSelectionChanged(object Sender, SelectionChangedEventArgs EventArgs)
        {
            PART_ListView.SelectedItem = null;
        }

        private void OnHorizontalThumbDragDelta(object Sender, System.Windows.Controls.Primitives.DragDeltaEventArgs EventArgs)
        {
            HeaderWidth = Math.Clamp(HeaderWidth + EventArgs.HorizontalChange, MinHeaderWidth, MaxHeaderWidth);
            EventArgs.Handled = true;
        }

        private void OnMinimizeAllClicked(object Sender, RoutedEventArgs EventArgs)
        {
            if (Timeline != null)
            {
                foreach (var Section in Timeline.Sections)
                {
                    Section.HeaderHeight = Section.MinHeaderHeight;
                }
            }
        }
    }
}
