using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace LogGenius.Modules.Entries
{
    public partial class EntryView : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(EntryView),
                new PropertyMetadata(string.Empty, OnTextChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
            if (Object is EntryView EntryTextBlock)
            {
                EntryTextBlock.UpdateHighlightText();
            }
        }

        public static readonly DependencyProperty HighlightTextProperty =
            DependencyProperty.Register(
                nameof(HighlightText),
                typeof(string),
                typeof(EntryView),
                new PropertyMetadata(string.Empty, OnHighlightTextChanged));

        public string HighlightText
        {
            get => (string)GetValue(HighlightTextProperty);
            set => SetValue(HighlightTextProperty, value);
        }

        private static void OnHighlightTextChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
            if (Object is EntryView EntryTextBlock)
            {
                EntryTextBlock.UpdateHighlightText();
            }
        }

        public static readonly DependencyProperty HighlightTextCaseSensitiveEnabledProperty =
            DependencyProperty.Register(
                nameof(HighlightTextCaseSensitiveEnabled),
                typeof(bool),
                typeof(EntryView),
                new PropertyMetadata(false, OnHighlightTextCaseSensitiveEnabledChanged));

        public bool HighlightTextCaseSensitiveEnabled
        {
            get => (bool)GetValue(HighlightTextCaseSensitiveEnabledProperty);
            set => SetValue(HighlightTextCaseSensitiveEnabledProperty, value);
        }

        private static void OnHighlightTextCaseSensitiveEnabledChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
            if (Object is EntryView EntryTextBlock)
            {
                EntryTextBlock.UpdateHighlightText();
            }
        }

        public static readonly DependencyProperty HighlightTextRegexEnabledProperty =
            DependencyProperty.Register(
                nameof(HighlightTextRegexEnabled),
                typeof(bool),
                typeof(EntryView),
                new PropertyMetadata(false, OnHighlightTextRegexEnabledChanged));

        public bool HighlightTextRegexEnabled
        {
            get => (bool)GetValue(HighlightTextRegexEnabledProperty);
            set => SetValue(HighlightTextRegexEnabledProperty, value);
        }

        private static void OnHighlightTextRegexEnabledChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
            if (Object is EntryView EntryTextBlock)
            {
                EntryTextBlock.UpdateHighlightText();
            }
        }

        public static readonly DependencyProperty HighlightTextBackgroundBrushProperty =
            DependencyProperty.Register(
                nameof(HighlightTextBackgroundBrush),
                typeof(Brush),
                typeof(EntryView),
                new PropertyMetadata(null, OnHighlightTextBackgroundBrushChanged));

        public Brush HighlightTextBackgroundBrush
        {
            get => (Brush)GetValue(HighlightTextBackgroundBrushProperty);
            set => SetValue(HighlightTextBackgroundBrushProperty, value);
        }

        private static void OnHighlightTextBackgroundBrushChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
            if (Object is EntryView EntryTextBlock)
            {
                EntryTextBlock.UpdateHighlightText();
            }
        }

        private class InlineInfo
        {
            public string Text { get; }

            public bool Highlighted { get; }

            public InlineInfo(string Text, bool Highlighted)
            {
                this.Text = Text;
                this.Highlighted = Highlighted;
            }
        }

        private void UpdateHighlightText()
        {
            PART_TextBlock.Inlines.Clear();
            if (string.IsNullOrEmpty(HighlightText))
            {
                PART_TextBlock.Inlines.Add(Text);
                return;
            }
            List<InlineInfo> InlineInfos = new();
            if (!HighlightTextRegexEnabled)
            {
                int StartPosition = 0;
                while (StartPosition < Text.Length)
                {
                    var Found = Text.IndexOf(HighlightText, StartPosition, HighlightTextCaseSensitiveEnabled ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
                    if (Found == -1)
                    {
                        InlineInfos.Add(new(Text.Substring(StartPosition), false));
                        break;
                    }
                    else
                    {
                        InlineInfos.Add(new(Text.Substring(StartPosition, Found - StartPosition), false));
                        InlineInfos.Add(new(Text.Substring(Found, HighlightText.Length), true));
                        StartPosition = Found + HighlightText.Length;
                    }
                }
            }
            else
            {
                var Regex = RegexCache.Get(HighlightText, HighlightTextCaseSensitiveEnabled);
                if (Regex == null)
                {
                    InlineInfos.Add(new(Text, false));
                }
                else
                {
                    var MatchCollection = Regex.Matches(Text);
                    int StartPosition = 0;
                    foreach (Match Match in MatchCollection)
                    {
                        if (Match.Index > StartPosition)
                        {
                            InlineInfos.Add(new(Text.Substring(StartPosition, Match.Index - StartPosition), false));
                        }
                        InlineInfos.Add(new(Match.Value, true));
                        StartPosition = Match.Index + Match.Length;
                    }
                    if (StartPosition < Text.Length)
                    {
                        InlineInfos.Add(new(Text.Substring(StartPosition, Text.Length - StartPosition), false));
                    }
                }
            }
            var BackgroundBrush = HighlightTextBackgroundBrush?.Clone();
            if (BackgroundBrush != null)
            {
                BackgroundBrush.Opacity = 0.3;
            }
            foreach (var InlineInfo in InlineInfos)
            {
                if (InlineInfo.Text.Length == 0)
                {
                    continue;
                }
                if (!InlineInfo.Highlighted)
                {
                    PART_TextBlock.Inlines.Add(InlineInfo.Text);
                }
                else
                {
                    PART_TextBlock.Inlines.Add(new Run(InlineInfo.Text) { Background = BackgroundBrush });
                }
            }
        }

        public EntryView()
        {
            InitializeComponent();
        }

        static TextBox? SelectableTextBox;

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (!e.Handled)
            {
                DismissSelectableTextBox();
                PopupSelectableTextBox();
            }
        }

        protected void TryInitializeSelectableTextBox()
        {
            if (SelectableTextBox != null)
            {
                return;
            }
            SelectableTextBox = (TextBox) this.Resources["SelectableTextBox"];
            var Padding = PART_TextBlock.Padding;
            Padding.Left--;
            Padding.Top--;
            Padding.Right--;
            Padding.Bottom--;
            SelectableTextBox.Padding = Padding;
            SelectableTextBox.Margin = PART_TextBlock.Margin;
            SelectableTextBox.MinHeight = PART_TextBlock.MinHeight;
            SelectableTextBox.FontFamily = PART_TextBlock.FontFamily;
            SelectableTextBox.FontSize = PART_TextBlock.FontSize;
        }

        private void OnWindowPreviewMouseDown(object Sender, MouseButtonEventArgs EventArgs)
        {
            if (SelectableTextBox == null)
            {
                return;
            }
            Point MousePosition = EventArgs.GetPosition(SelectableTextBox);
            if (MousePosition.X >= 0 &&
                MousePosition.Y >= 0 &&
                MousePosition.X <= SelectableTextBox.ActualWidth &&
                MousePosition.Y <= SelectableTextBox.ActualHeight)
            {
                return;
            }
            DismissSelectableTextBox();
        }

        protected void PopupSelectableTextBox()
        {
            TryInitializeSelectableTextBox();
            SelectableTextBox!.Text = Text;
            PART_TextBlock.Visibility = Visibility.Hidden;
            PART_PopupSlot.Children.Add(SelectableTextBox);
            var Window = ModernWpf.VisualTree.FindAscendant<Window>(this);
            Window.PreviewMouseDown += OnWindowPreviewMouseDown;
            Mouse.Capture(SelectableTextBox);
        }

        protected static void DismissSelectableTextBox()
        {
            if (SelectableTextBox == null)
            {
                return;
            }
            var ParentPanel = SelectableTextBox?.Parent as Panel;
            if (ParentPanel == null)
            {
                return;
            }
            var EntryView = ModernWpf.VisualTree.FindAscendant<EntryView>(ParentPanel);
            if (EntryView != null)
            {
                Mouse.Capture(null);
                var Window = ModernWpf.VisualTree.FindAscendant<Window>(EntryView);
                Window.PreviewMouseDown -= EntryView.OnWindowPreviewMouseDown;
                EntryView.PART_PopupSlot.Children.Clear();
                EntryView.PART_TextBlock.Visibility = Visibility.Visible;
            }
        }
    }
}
