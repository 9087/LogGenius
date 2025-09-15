using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
    }
}
