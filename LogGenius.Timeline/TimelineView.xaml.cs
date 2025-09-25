using Serilog;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

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

    public class GetTutorialVisiblityFromSectionCount : IValueConverter
    {
        public object Convert(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            if (Value is int Integer)
            {
                return Integer == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object Value, Type TargetType, object Parameter, CultureInfo Culture)
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

        public Timeline Timeline
        {
            get => (Timeline)GetValue(TimelineProperty);
            set => SetValue(TimelineProperty, value);
        }

        private static void OnTimelineChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
            if (Object is TimelineView TimelineView)
            {
                if (EventArgs.OldValue is Timeline OldTimeline)
                {
                    OldTimeline.PropertyChanged -= TimelineView.OnTimelinePropertyChanged;
                }
                if (EventArgs.NewValue is Timeline NewTimeline)
                {
                    NewTimeline.PropertyChanged += TimelineView.OnTimelinePropertyChanged;
                }
            }
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

        public double Offset => PART_ScrollBar.Value;

        public TimelineView()
        {
            InitializeComponent();
        }

        ~TimelineView()
        {
            if (Timeline != null)
            {
                Timeline.PropertyChanged -= OnTimelinePropertyChanged;
            }
        }

        private void OnTimelinePropertyChanged(object? Sender, System.ComponentModel.PropertyChangedEventArgs EventArgs)
        {
            if (EventArgs.PropertyName == nameof(Timeline.MillisecondPerPixel))
            {
                UpdateRuler();
            }
        }

        protected void UpdateRuler()
        {
            this.PART_Ruler.Children.Clear();
            var InitialMillisecond = Timeline.GetMillisecondByHorizontal(0, Offset);
            var Current = (int)(InitialMillisecond / Timeline.RulerMillisecondSpacing);
            double Horizontal;
            do
            {
                var Millisecond = (double)Current * Timeline.RulerMillisecondSpacing;
                Horizontal = Timeline.GetHorizontalByMillisecond(Millisecond, Offset);
                double ShortRulerMarkPercentHeight = 0.2;
                double LongRulerMarkPercentHeight = 0.3;
                if (Current != 0)
                {
                    var RulerMarkPercentHeight = Current % Timeline.RulerCountPerTimeTextBlock == 0
                        ? LongRulerMarkPercentHeight
                        : ShortRulerMarkPercentHeight;
                    this.PART_Ruler.Children.Add(new Line()
                    {
                        X1 = Horizontal,
                        X2 = Horizontal,
                        Y1 = this.PART_Ruler.ActualHeight * (1 - RulerMarkPercentHeight),
                        Y2 = this.PART_Ruler.ActualHeight * 1.0,
                        StrokeThickness = 1,
                        Stroke = Brushes.Black,
                    });
                    
                    if (Current % Timeline.RulerCountPerTimeTextBlock == 0)
                    {
                        var TimeSpan = new TimeSpan(0, 0, 0, 0, (int)Millisecond);
                        var TimeTextBlock = new TextBlock()
                        {
                            Text = $"{(int)TimeSpan.TotalMinutes}.{TimeSpan.Seconds:D2}:{TimeSpan.Milliseconds:D3}",
                            TextAlignment = TextAlignment.Left,
                            FontSize = 10,
                            VerticalAlignment = VerticalAlignment.Center,
                        };
                        TimeTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        TimeTextBlock.Arrange(new Rect(0, 0, TimeTextBlock.DesiredSize.Width, TimeTextBlock.DesiredSize.Height));

                        this.PART_Ruler.Children.Add(TimeTextBlock);
                        Canvas.SetLeft(TimeTextBlock, Horizontal - TimeTextBlock.ActualWidth * 0.5);
                        Canvas.SetTop(TimeTextBlock, this.PART_Ruler.ActualHeight * (1 - RulerMarkPercentHeight) * 0.5 - TimeTextBlock.ActualHeight * 0.5);
                    }
                }
                Current += 1;
            } while (Horizontal < this.PART_Ruler.ActualWidth);
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
            UpdateRuler();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo SizeInfo)
        {
            base.OnRenderSizeChanged(SizeInfo);
            UpdateRuler();
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

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
        }
    }
}
