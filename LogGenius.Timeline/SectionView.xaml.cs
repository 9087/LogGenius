using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LogGenius.Modules.Timeline
{
    public partial class SectionView : UserControl
    {
        public static readonly DependencyProperty TimelineProperty =
            DependencyProperty.Register(
                nameof(Timeline),
                typeof(Timeline),
                typeof(SectionView),
                new PropertyMetadata(null, OnTimelineChanged));

        public Timeline? Timeline
        {
            get => (Timeline)GetValue(TimelineProperty);
            set => SetValue(TimelineProperty, value);
        }

        private static void OnTimelineChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
            if (Object is SectionView SectionView)
            {
                if (EventArgs.OldValue is Timeline OldTimeline)
                {
                    OldTimeline.PropertyChanged -= SectionView.OnTimelinePropertyChanged;
                }
                if (EventArgs.NewValue is Timeline NewTimeline)
                {
                    NewTimeline.PropertyChanged += SectionView.OnTimelinePropertyChanged;
                }
            }
        }

        private void OnTimelinePropertyChanged(object? Sender, System.ComponentModel.PropertyChangedEventArgs EventArgs)
        {
            if (EventArgs.PropertyName == nameof(Timeline.MillisecondPerPixel))
            {
                UpdateCanvas();
            }
        }

        public static readonly DependencyProperty SectionProperty =
            DependencyProperty.Register(
                nameof(Section),
                typeof(Section),
                typeof(SectionView),
                new PropertyMetadata(null, OnSectionChanged));

        public Section? Section
        {
            get => (Section)GetValue(SectionProperty);
            set => SetValue(SectionProperty, value);
        }

        private static void OnSectionChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
            if (Object is SectionView SectionView)
            {
                if (EventArgs.OldValue is Section OldSection)
                {
                    OldSection.RecordAdded -= SectionView.OnSectionRecordAdded;
                }
                SectionView.UpdateCanvas();
                if (EventArgs.OldValue is Section NewSection)
                {
                    NewSection.RecordAdded += SectionView.OnSectionRecordAdded;
                }
            }
        }

        public static readonly DependencyProperty OffsetProperty =
            DependencyProperty.Register(
                nameof(Offset),
                typeof(double),
                typeof(SectionView),
                new PropertyMetadata((double)0, OnOffsetChanged));

        public double Offset
        {
            get => (double)GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        private static void OnOffsetChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
            if (Object is SectionView SectionView)
            {
                SectionView.UpdateCanvas();
            }
        }

        public static readonly DependencyProperty HeaderWidthProperty =
            DependencyProperty.Register(
                nameof(HeaderWidth),
                typeof(double),
                typeof(SectionView),
                new PropertyMetadata(100.0, OnHeaderWidthChanged));

        public double? HeaderWidth
        {
            get => (double)GetValue(HeaderWidthProperty);
            set => SetValue(HeaderWidthProperty, value);
        }

        private static void OnHeaderWidthChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
        }

        public static readonly DependencyProperty HeaderHeightProperty =
            DependencyProperty.Register(
                nameof(HeaderHeight),
                typeof(double),
                typeof(SectionView),
                new PropertyMetadata(60.0, OnHeaderHeightChanged));

        public double HeaderHeight
        {
            get => (double)GetValue(HeaderHeightProperty);
            set => SetValue(HeaderHeightProperty, value);
        }

        private static void OnHeaderHeightChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
        }

        public static readonly DependencyProperty MinHeaderHeightProperty =
            DependencyProperty.Register(
                nameof(MinHeaderHeight),
                typeof(double),
                typeof(SectionView),
                new PropertyMetadata(30.0));

        public double MinHeaderHeight
        {
            get => (double)GetValue(MinHeaderHeightProperty);
            set => SetValue(MinHeaderHeightProperty, value);
        }

        public static readonly DependencyProperty MaxHeaderHeightProperty =
            DependencyProperty.Register(
                nameof(MaxHeaderHeight),
                typeof(double),
                typeof(SectionView),
                new PropertyMetadata(800.0));

        public double MaxHeaderHeight
        {
            get => (double)GetValue(MaxHeaderHeightProperty);
            set => SetValue(MaxHeaderHeightProperty, value);
        }

        private Style? RecordMarkButtonStyle;

        public SectionView()
        {
            InitializeComponent();
            RecordMarkButtonStyle = TryFindResource("RecordMarkButtonStyle") as Style;
        }

        ~SectionView()
        {
            if (Section != null)
            {
                Section.RecordAdded -= OnSectionRecordAdded;
                Section = null;
            }
            if (Timeline != null)
            {
                Timeline.PropertyChanged -= OnTimelinePropertyChanged;
                Timeline = null;
            }
        }

        protected void OnSectionRecordAdded(PropertyRecord Record)
        {
            UpdateCanvas();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateCanvas();
        }

        public void Invalidate()
        {
            UpdateCanvas();
        }

        private int FindEarliestKeyFrameIndexAfterTime(DateTime Time, int StartIndex, int EndIndex)
        {
            if (StartIndex == EndIndex)
            {
                return StartIndex;
            }
            var MiddleIndex = (StartIndex + EndIndex) / 2;
            if (Section!.KeyFrames[MiddleIndex].DateTime < Time)
            {
                return FindEarliestKeyFrameIndexAfterTime(Time, MiddleIndex + 1, EndIndex);
            }
            else
            {
                return FindEarliestKeyFrameIndexAfterTime(Time, StartIndex, MiddleIndex);
            }
        }

        private int FindEarliestKeyFrameIndexAfterTime(DateTime Time)
        {
            if (Section == null || Section.KeyFrames.Count == 0)
            {
                return -1;
            }
            return FindEarliestKeyFrameIndexAfterTime(Time, 0, Section.KeyFrames.Count - 1);
        }

        private void UpdateCanvas()
        {
            PART_Canvas.Children.Clear();
            if (Section == null || this.Timeline == null)
            {
                return;
            }
            var Timeline = (Timeline)this.Timeline!;
            var Identity = (PropertyIdentity)Section.Identity!;
            var Lower = (double)Identity.Lower!;
            var Upper = (double)Identity.Upper!;
            var ValueLength = Upper - Lower;
            var InitialTime = (DateTime)Timeline.InitialTime!;
            var ZeroTime = InitialTime + new TimeSpan(0, 0, 0, 0, (int)Math.Ceiling(Offset * Timeline.MillisecondPerPixel));

            var EarliestKeyFrameIndex = FindEarliestKeyFrameIndexAfterTime(ZeroTime);
            if (EarliestKeyFrameIndex < 0)
            {
                return;
            }

            double GetHorizontalByTime(DateTime Time)
            {
                return -Offset + (Time - InitialTime).TotalMilliseconds / Timeline.MillisecondPerPixel;
            }
            double GetVerticalByValue(double Value)
            {
                float PaddingTop = 10;
                float PaddingBottom = 10;
                if (ValueLength == 0)
                {
                    return PaddingTop;
                }
                return (1 - (Value - Lower) / ValueLength) * (this.PART_Canvas.ActualHeight - PaddingTop - PaddingBottom) + PaddingTop;
            }

            int VisibleFirstIndex = Math.Max(0, EarliestKeyFrameIndex - 1);
            int VisibleLastIndex = Section.KeyFrames.Count - 1;

            for (int Index = VisibleFirstIndex; Index < VisibleLastIndex; Index++)
            {
                var PreviousKeyFrame = (KeyFrame)Section.KeyFrames[Index]!;
                var LastTime = PreviousKeyFrame.DateTime;
                var LastRecord = (PropertyRecord)PreviousKeyFrame.Records.Last()!;
                var CurrentKeyFrame = (KeyFrame)Section.KeyFrames[Index + 1]!;

                bool Finished = false;
                foreach (PropertyRecord CurrentRecord in CurrentKeyFrame.Records)
                {
                    var X1 = GetHorizontalByTime(LastTime);
                    var X2 = GetHorizontalByTime(CurrentKeyFrame.DateTime);
                    var Y1 = GetVerticalByValue(LastRecord.Value);
                    var Y2 = GetVerticalByValue(CurrentRecord.Value);
                    PART_Canvas.Children.Add(
                        new Line()
                        {
                            X1 = X1,
                            Y1 = Y1,
                            X2 = X2,
                            Y2 = Y2,
                            Stroke = new SolidColorBrush(Colors.Gray),
                            StrokeThickness = 2,
                        }
                    );
                    LastTime = CurrentKeyFrame.DateTime;
                    LastRecord = CurrentRecord;
                    Finished |= X2 > this.PART_Canvas.ActualWidth;
                }
                if (Finished)
                {
                    VisibleLastIndex = Index + 1;
                    break;
                }
            }
            VisibleLastIndex = Math.Max(VisibleLastIndex, VisibleFirstIndex);
            for (int Index = VisibleFirstIndex; Index <= VisibleLastIndex; Index++)
            {
                var CurrentKeyFrame = (KeyFrame)Section.KeyFrames[Index]!;
                foreach (var CurrentRecord in CurrentKeyFrame.Records)
                {
                    var X = GetHorizontalByTime(CurrentKeyFrame.DateTime);
                    var Y = GetVerticalByValue(CurrentRecord.Value);
                    var Button = new Button()
                    {
                        Style = RecordMarkButtonStyle,
                        DataContext = CurrentRecord,
                    };
                    if (RecordMarkButtonStyle != null)
                    {
                        Button.Style = RecordMarkButtonStyle;
                    }
                    PART_Canvas.Children.Add(Button);
                    Canvas.SetLeft(Button, X - Button.Width * 0.5);
                    Canvas.SetTop(Button, Y - Button.Height * 0.5);
                }
            }
        }

        private void OnVerticalThumbDragDelta(object Sender, System.Windows.Controls.Primitives.DragDeltaEventArgs EventArgs)
        {
            HeaderHeight = Math.Clamp(HeaderHeight + EventArgs.VerticalChange, MinHeaderHeight, MaxHeaderHeight);
            EventArgs.Handled = true;
        }

        private void OnVerticalThumbMouseDoubleClicked(object Sender, System.Windows.Input.MouseButtonEventArgs EventArgs)
        {
            if (Section != null)
            {
                Section.HeaderHeight = Section.MaxHeaderHeight;
            }
        }
    }
}
