using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LogGenius.Modules.Timeline
{
    public partial class TrackView : UserControl
    {
        public static readonly DependencyProperty TimelineProperty =
            DependencyProperty.Register(
                nameof(Timeline),
                typeof(Timeline),
                typeof(TrackView),
                new PropertyMetadata(null, OnTimelineChanged));

        public Timeline? Timeline
        {
            get => (Timeline)GetValue(TimelineProperty);
            set => SetValue(TimelineProperty, value);
        }

        private static void OnTimelineChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
            if (Object is TrackView TrackView)
            {
                if (EventArgs.OldValue is Timeline OldTimeline)
                {
                    OldTimeline.PropertyChanged -= TrackView.OnTimelinePropertyChanged;
                }
                if (EventArgs.NewValue is Timeline NewTimeline)
                {
                    NewTimeline.PropertyChanged += TrackView.OnTimelinePropertyChanged;
                }
            }
        }

        private void OnTimelinePropertyChanged(object? Sender, System.ComponentModel.PropertyChangedEventArgs EventArgs)
        {
            if (EventArgs.PropertyName == nameof(Timeline.MillisecondPerPixel))
            {
                UpdateCurves();
            }
        }

        public static readonly DependencyProperty TrackProperty =
            DependencyProperty.Register(
                nameof(Track),
                typeof(Track),
                typeof(TrackView),
                new PropertyMetadata(null, OnTrackChanged));

        public Track? Track
        {
            get => (Track)GetValue(TrackProperty);
            set => SetValue(TrackProperty, value);
        }

        private static void OnTrackChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
            if (Object is TrackView TrackView)
            {
                if (EventArgs.OldValue is Track OldTrack)
                {
                    OldTrack.RecordAdded -= TrackView.OnTrackRecordAdded;
                }
                TrackView.UpdateCurves();
                if (EventArgs.NewValue is Track NewTrack)
                {
                    NewTrack.RecordAdded += TrackView.OnTrackRecordAdded;
                }
            }
        }

        public static readonly DependencyProperty OffsetProperty =
            DependencyProperty.Register(
                nameof(Offset),
                typeof(double),
                typeof(TrackView),
                new PropertyMetadata((double)0, OnOffsetChanged));

        public double Offset
        {
            get => (double)GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        private static void OnOffsetChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
            if (Object is TrackView TrackView)
            {
                TrackView.UpdateCurves();
            }
        }

        public static readonly DependencyProperty HeaderWidthProperty =
            DependencyProperty.Register(
                nameof(HeaderWidth),
                typeof(double),
                typeof(TrackView),
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
                typeof(TrackView),
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
                typeof(TrackView),
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
                typeof(TrackView),
                new PropertyMetadata(800.0));

        public double MaxHeaderHeight
        {
            get => (double)GetValue(MaxHeaderHeightProperty);
            set => SetValue(MaxHeaderHeightProperty, value);
        }

        private Style? RecordMarkButtonStyle;

        public TrackView()
        {
            InitializeComponent();
            RecordMarkButtonStyle = TryFindResource("RecordMarkButtonStyle") as Style;
        }

        ~TrackView()
        {
            if (Track != null)
            {
                Track.RecordAdded -= OnTrackRecordAdded;
                Track = null;
            }
            if (Timeline != null)
            {
                Timeline.PropertyChanged -= OnTimelinePropertyChanged;
                Timeline = null;
            }
        }

        protected void OnTrackRecordAdded(PropertyRecord Record)
        {
            UpdateCurves();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo SizeInfo)
        {
            base.OnRenderSizeChanged(SizeInfo);
            UpdateCurves();
        }

        public void Invalidate()
        {
            UpdateCurves();
        }

        private int FindEarliestKeyFrameIndexAfterTime(DateTime Time, int StartIndex, int EndIndex)
        {
            if (StartIndex == EndIndex)
            {
                return StartIndex;
            }
            var MiddleIndex = (StartIndex + EndIndex) / 2;
            if (Track!.KeyFrames[MiddleIndex].DateTime < Time)
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
            if (Track == null || Track.KeyFrames.Count == 0)
            {
                return -1;
            }
            return FindEarliestKeyFrameIndexAfterTime(Time, 0, Track.KeyFrames.Count - 1);
        }

        private Timer? UpdateCurvesWaitingTimer = null;
        
        private void UpdateCurves()
        {
            if (UpdateCurvesWaitingTimer != null)
            {
                return;
            }
            UpdateCurvesWaitingTimer = new Timer(_ =>
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    UpdateCurvesInternal();
                    this.UpdateCurvesWaitingTimer = null;
                });
            }, null, TimelineModule.Instance.CurveUpdateInterval, 0);
        }

        private List<Button> ButtonCache = new();

        private void UpdateCurvesInternal()
        {
            PART_Canvas.Children.Clear();
            if (Track == null || this.Timeline == null)
            {
                return;
            }
            var Timeline = (Timeline)this.Timeline!;
            var Identity = (PropertyIdentity)Track.Identity!;
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
            int VisibleLastIndex = Track.KeyFrames.Count - 1;

            bool UsePathGeometry = true;
            bool UseEllipseGeometry = false;

            // Line
            {
                var FirstKeyFrame = (KeyFrame)Track.KeyFrames[VisibleFirstIndex]!;
                var FirstTime = FirstKeyFrame.DateTime;
                var HeadRecord = (PropertyRecord)FirstKeyFrame.Records.Last()!;

                var PathFigure = new PathFigure()
                {
                    StartPoint = new Point()
                    {
                        X = Timeline.GetHorizontalByTime(FirstTime, Offset),
                        Y = GetVerticalByValue(HeadRecord.Value),
                    }
                };

                for (int Index = VisibleFirstIndex; Index < VisibleLastIndex; Index++)
                {
                    var PreviousKeyFrame = (KeyFrame)Track.KeyFrames[Index]!;
                    var LastTime = PreviousKeyFrame.DateTime;
                    var LastRecord = (PropertyRecord)PreviousKeyFrame.Records.Last()!;
                    var CurrentKeyFrame = (KeyFrame)Track.KeyFrames[Index + 1]!;

                    bool Finished = false;
                    foreach (PropertyRecord CurrentRecord in CurrentKeyFrame.Records)
                    {
                        var X2 = Timeline.GetHorizontalByTime(CurrentKeyFrame.DateTime, Offset);
                        var Y2 = GetVerticalByValue(CurrentRecord.Value);

                        if (UsePathGeometry)
                        {
                            PathFigure.Segments.Add(new LineSegment(new Point(X2, Y2), true));
                        }
                        else
                        {
                            var X1 = Timeline.GetHorizontalByTime(LastTime, Offset);
                            var Y1 = GetVerticalByValue(LastRecord.Value);
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
                        }

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
                if (UsePathGeometry)
                {
                    var PathGeometry = new PathGeometry();
                    PathGeometry.Figures.Add(PathFigure);
                    var Path = new Path()
                    {
                        Data = PathGeometry,
                        Stroke = new SolidColorBrush(Colors.Gray),
                        StrokeThickness = 2,
                    };
                    PART_Canvas.Children.Add(Path);
                }
            }

            // Circle
            {
                var GeometryGroup = UseEllipseGeometry ? new GeometryGroup() : null;
                int ButtonCacheIndex = 0;

                VisibleLastIndex = Math.Max(VisibleLastIndex, VisibleFirstIndex);
                for (int Index = VisibleFirstIndex; Index <= VisibleLastIndex; Index++)
                {
                    var CurrentKeyFrame = (KeyFrame)Track.KeyFrames[Index]!;
                    foreach (var CurrentRecord in CurrentKeyFrame.Records)
                    {
                        var X = Timeline.GetHorizontalByTime(CurrentKeyFrame.DateTime, Offset);
                        var Y = GetVerticalByValue(CurrentRecord.Value);
                        if (UseEllipseGeometry)
                        {
                            GeometryGroup!.Children.Add(new EllipseGeometry(new Point(X, Y), 4, 4));
                        }
                        else
                        {
                            Button? Button = null;
                            if (ButtonCacheIndex >= ButtonCache.Count)
                            {
                                Debug.Assert(ButtonCacheIndex == ButtonCache.Count);
                                Button = new Button()
                                {
                                    Style = RecordMarkButtonStyle,
                                    DataContext = CurrentRecord,
                                };
                                ButtonCache.Add(Button);
                                PART_ButtonCacheCanvas.Children.Add(Button);
                            }
                            else
                            {
                                Button = ButtonCache[ButtonCacheIndex];
                                Button.DataContext = CurrentRecord;
                            }
                            ButtonCacheIndex++;
                            Button.RenderTransform = new TranslateTransform
                            {
                                X = X - Button.Width * 0.5,
                                Y = Y - Button.Height * 0.5,
                            };
                        }
                    }
                }
                for (int Index = ButtonCacheIndex; Index < ButtonCache.Count; Index++)
                {
                    var Button = ButtonCache[Index];
                    Button.RenderTransform = new TranslateTransform
                    {
                        X = -Button.Width,
                        Y = -Button.Height,
                    };
                }
                if (UseEllipseGeometry)
                {
                    var Path = new Path
                    {
                        Data = GeometryGroup,
                        Fill = Brushes.LightGray,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 2,
                    };
                    PART_Canvas.Children.Add(Path);
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
            if (Track != null)
            {
                Track.HeaderHeight = Track.MaxHeaderHeight;
            }
        }
    }
}
