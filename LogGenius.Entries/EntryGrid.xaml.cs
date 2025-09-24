using LogGenius.Core;
using Serilog;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LogGenius.Modules.Entries
{
    public class EntryDoubleClickedEventArgs : EventArgs
    {
        public Entry Item { get; }

        public EntryDoubleClickedEventArgs(Entry Item)
        {
            this.Item = Item;
        }
    }

    public delegate void EntryDoubleClickedEventHandler(object Sender, EntryDoubleClickedEventArgs EventArgs);

    public partial class EntryGrid : UserControl
    {
        #region HighlightText

        public static readonly DependencyProperty HighlightTextProperty =
            DependencyProperty.Register(
                nameof(HighlightText),
                typeof(string),
                typeof(EntryGrid),
                new PropertyMetadata(string.Empty, OnHighlightTextChanged));

        public string? HighlightText
        {
            get => (string?)GetValue(HighlightTextProperty);
            set => SetValue(HighlightTextProperty, value);
        }

        private static void OnHighlightTextChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
        }

        #endregion

        #region HighlightTextCaseSensitiveEnabled

        public static readonly DependencyProperty HighlightTextCaseSensitiveEnabledProperty =
            DependencyProperty.Register(
                nameof(HighlightTextCaseSensitiveEnabled),
                typeof(bool),
                typeof(EntryGrid),
                new PropertyMetadata(false, OnHighlightTextCaseSensitiveEnabledChanged));

        public bool HighlightTextCaseSensitiveEnabled
        {
            get => (bool)GetValue(HighlightTextCaseSensitiveEnabledProperty);
            set => SetValue(HighlightTextCaseSensitiveEnabledProperty, value);
        }

        private static void OnHighlightTextCaseSensitiveEnabledChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
        }

        #endregion

        #region HighlightTextRegexEnabled

        public static readonly DependencyProperty HighlightTextRegexEnabledProperty =
            DependencyProperty.Register(
                nameof(HighlightTextRegexEnabled),
                typeof(bool),
                typeof(EntryGrid),
                new PropertyMetadata(false, OnHighlightTextRegexEnabledChanged));

        public bool HighlightTextRegexEnabled
        {
            get => (bool)GetValue(HighlightTextRegexEnabledProperty);
            set => SetValue(HighlightTextRegexEnabledProperty, value);
        }

        private static void OnHighlightTextRegexEnabledChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
        }

        #endregion

        public static readonly DependencyProperty SelectableStateEnabledProperty =
            DependencyProperty.Register(
                nameof(SelectableStateEnabled),
                typeof(bool),
                typeof(EntryGrid),
                new PropertyMetadata(true));

        public bool SelectableStateEnabled
        {
            get => (bool)GetValue(SelectableStateEnabledProperty);
            set => SetValue(SelectableStateEnabledProperty, value);
        }

        public event EntryDoubleClickedEventHandler? EntryDoubleClicked;

        private bool IsScrolledToEnd = true;

        private ScrollViewer? ScrollViewer = null;

        public EntryGrid()
        {
            InitializeComponent();
            Manager.Instance.Session.EntriesRefreshed += OnEntriesRefreshed;
        }

        ~EntryGrid()
        {
            Manager.Instance.Session.EntriesRefreshed -= OnEntriesRefreshed;
        }

        private void OnEntriesRefreshed()
        {
            if (IsScrolledToEnd)
            {
                ScrollViewer ??= ModernWpf.VisualTree.FindDescendant<ScrollViewer>(PART_DataGrid);
                ScrollViewer.ScrollToBottom();
            }
        }

        private void OnDataGridSelectedCellsChanged(object Sender, SelectedCellsChangedEventArgs EventArgs)
        {
            if (!(Sender is DataGrid DataGrid))
            {
                return;
            }
            if (DataGrid.SelectedItem == null)
            {
                return;
            }
            ScrollViewer ??= ModernWpf.VisualTree.FindDescendant<ScrollViewer>(DataGrid);
            if (ScrollViewer != null)
            {
                double LastHorizontalOffset = ScrollViewer.HorizontalOffset;
                Dispatcher.BeginInvoke(() =>
                {
                    DataGrid.ScrollIntoView(DataGrid.SelectedItem);
                    ScrollViewer.ScrollToHorizontalOffset(LastHorizontalOffset);
                });
            }
        }

        public void SetSelectedItem(object Item)
        {
            PART_DataGrid.SelectedItem = Item;
        }

        public void ScrollIntoView(object Item)
        {
            PART_DataGrid.ScrollIntoView(Item);
        }

        private void OnDataGridRowMouseDoubleClicked(object Sender, System.Windows.Input.MouseButtonEventArgs EventArgs)
        {
            if (Sender is DataGridRow Row)
            {
                EntryDoubleClicked?.Invoke(Sender, new EntryDoubleClickedEventArgs((Entry)Row.DataContext));
            }
        }

        private void OnDataGridScrollChanged(object Sender, ScrollChangedEventArgs EventArgs)
        {
            IsScrolledToEnd = EventArgs.VerticalOffset + EventArgs.ViewportHeight >= EventArgs.ExtentHeight - 1;
        }
    }
}
