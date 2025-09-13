using LogGenius.Core;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace LogGenius.Modules.Timeline
{
    public class DataGridHelper
    {
        private static readonly Dictionary<DataGrid, NotifyCollectionChangedEventHandler> Handlers;

        static DataGridHelper()
        {
            Handlers = new Dictionary<DataGrid, NotifyCollectionChangedEventHandler>();
        }

        public static readonly DependencyProperty BindableColumnsProperty =
            DependencyProperty.RegisterAttached(
                "BindableColumns",
                typeof(ObservableCollection<DataGridColumn>),
                typeof(DataGridHelper),
                new UIPropertyMetadata(null, OnBindableColumnsPropertyChanged)
            );

        private static void OnBindableColumnsPropertyChanged(DependencyObject Source, DependencyPropertyChangedEventArgs EventArgs)
        {
            if (!(Source is DataGrid DataGrid)) return;
            if (EventArgs.OldValue is ObservableCollection<DataGridColumn> OldColumns)
            {
                DataGrid.Columns.Clear();
                if (Handlers.TryGetValue(DataGrid, out var Handler))
                {
                    OldColumns.CollectionChanged -= Handler;
                    Handlers.Remove(DataGrid);
                }
            }
            var NewColumns = EventArgs.NewValue as ObservableCollection<DataGridColumn>;
            DataGrid.Columns.Clear();
            if (NewColumns != null)
            {
                foreach (var Column in NewColumns)
                {
                    if (Column == null)
                    {
                        continue;
                    }
                    var OldDataGrid = (DataGrid?)Column.GetType().GetProperty("DataGridOwner", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(Column, null);
                    OldDataGrid?.Columns.Clear();
                    DataGrid.Columns.Add(Column);
                }
                NotifyCollectionChangedEventHandler Handler = (_, EventArgs) => OnCollectionChanged(EventArgs, DataGrid);
                Handlers[DataGrid] = Handler;
                NewColumns.CollectionChanged += Handler;
            }
        }

        private static void OnCollectionChanged(NotifyCollectionChangedEventArgs EventArgs, DataGrid DataGrid)
        {
            switch (EventArgs.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    DataGrid.Columns.Clear();
                    if (EventArgs.NewItems != null && EventArgs.NewItems.Count > 0)
                    {
                        foreach (DataGridColumn Column in EventArgs.NewItems)
                        {
                            DataGrid.Columns.Add(Column);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Add:
                    foreach (DataGridColumn Column in EventArgs.NewItems!)
                    {
                        DataGrid.Columns.Add(Column);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    DataGrid.Columns.Move(EventArgs.OldStartingIndex, EventArgs.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (DataGridColumn column in EventArgs.OldItems!)
                    {
                        DataGrid.Columns.Remove(column);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    DataGrid.Columns[EventArgs.NewStartingIndex] = EventArgs.NewItems![0] as DataGridColumn;
                    break;
            }
        }

        public static void SetBindableColumns(DependencyObject Element, ObservableCollection<DataGridColumn> Value)
        {
            Element.SetValue(BindableColumnsProperty, Value);
        }

        public static ObservableCollection<DataGridColumn> GetBindableColumns(DependencyObject Element)
        {
            return (ObservableCollection<DataGridColumn>)Element.GetValue(BindableColumnsProperty);
        }
    }

    [WindowInfo("Timeline", 2, false)]
    public partial class TimelineWindow : LogGenius.Core.Window
    {
        public TimelineWindow()
        {
            InitializeComponent();
        }

        private void OnDataGridColumnHeaderLoaded(object Sender, RoutedEventArgs EventArgs)
        {
            var DataGridColumnHeader = Sender as DataGridColumnHeader;
            if (DataGridColumnHeader != null)
            {
                var LeftHeaderGripper = DataGridColumnHeader.Template.FindName("PART_LeftHeaderGripper", DataGridColumnHeader) as Thumb;
                if (LeftHeaderGripper != null)
                {
                    LeftHeaderGripper.HorizontalAlignment = HorizontalAlignment.Stretch;
                    LeftHeaderGripper.VerticalAlignment = VerticalAlignment.Top;
                    LeftHeaderGripper.LayoutTransform = new RotateTransform(90);
                    LeftHeaderGripper.Cursor = Cursors.SizeNS;
                }
                var RightHeaderGripper = DataGridColumnHeader.Template.FindName("PART_RightHeaderGripper", DataGridColumnHeader) as Thumb;
                if (RightHeaderGripper != null)
                {
                    RightHeaderGripper.HorizontalAlignment = HorizontalAlignment.Stretch;
                    RightHeaderGripper.VerticalAlignment = VerticalAlignment.Bottom;
                    RightHeaderGripper.LayoutTransform = new RotateTransform(90);
                    RightHeaderGripper.Cursor = Cursors.SizeNS;
                }
            }
        }
    }
}
