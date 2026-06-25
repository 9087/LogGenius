using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace LogGenius.Core
{
    public class EntryObservableCollection : ObservableCollection<Entry>
    {
        public class BatchOperation : IDisposable
        {
            private EntryObservableCollection Collection;

            public BatchOperation(EntryObservableCollection Collection)
            {
                Debug.Assert(Collection._BatchOperation == null);
                this.Collection = Collection;
                this.Collection._BatchOperation = this;
            }

            public void Dispose()
            {
                this.Collection.EndBatchOperation();
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs EventArgs)
        {
            if (_BatchOperation != null)
            {
                return;
            }
            base.OnCollectionChanged(EventArgs);
        }

        public BatchOperation BeginBatchOperation()
        {
            if (_BatchOperation != null)
            {
                throw new InvalidOperationException();
            }
            return new BatchOperation(this);
        }

        protected virtual void EndBatchOperation()
        {
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            this._BatchOperation = null;
        }

        private BatchOperation? _BatchOperation = null;
    }

    public partial class Session : ObservableObject
    {
        [ObservableProperty]
        protected EntryObservableCollection _Entries = new();

        public int Interval => CoreModule.Instance.UpdateInterval;

        public int BufferSize => CoreModule.Instance.UpdateBufferSize;

        private StreamReader? Reader { get; set; } = null;

        public Action? EntriesRefreshed { get; set; }

        public Action<List<Entry>>? EntriesAdded { get; set; }

        public Action? EntriesCleared { get; set; }

        private Task? UpdatingTask = null;

        private CancellationTokenSource? UpdatingTaskCancellationTokenSource;

        public Action<Entry>? EntryCreated { get; set; }

        [ObservableProperty]
        private string? _FilePath = null;

        #region Coalesce / Backpressure

        private List<Entry> PendingEntries = new();

        private readonly object PendingEntriesLock = new();

        private bool IsFlushScheduled = false;

        private DispatcherTimer? FlushTimer;

        #endregion

        public Session()
        {
            FlushTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50),
            };
            FlushTimer.Tick += OnFlushTimerTick;
        }

        public Session(string FilePath)
        {
            OpenFile(FilePath);
        }

        public bool IsFileOpened => this.UpdatingTask != null && this.Reader != null && FilePath != null;

        [RelayCommand]
        public void OpenFileFromDialog()
        {
            var Dialog = new OpenFileDialog();
            Dialog.Title = "Open File...";
            Dialog.Multiselect = false;
            var Result = Dialog.ShowDialog();
            if (Result == null || (!(bool)Result))
            {
                return;
            }
            OpenFile(Dialog.FileName);
        }

        [RelayCommand]
        public void OpenFile(string FilePath)
        {
            if (IsFileOpened)
            {
                CloseFile();
            }
            if (FilePath == null)
            {
                throw new ArgumentNullException(nameof(FilePath));
            }
            if (!File.Exists(FilePath))
            {
                throw new FileNotFoundException(nameof(FilePath));
            }
            ClearEntries();
            try
            {
                this.FilePath = FilePath;
                var Stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                this.Reader = new StreamReader(Stream);
                this.UpdatingTaskCancellationTokenSource = new();
                this.UpdatingTask = Task.Run(() => Update(this.UpdatingTaskCancellationTokenSource.Token));
            }
            catch (Exception)
            {
                this.CloseFile();
                throw;
            }
            CoreModule.Instance.RaiseOnFileOpened(Path.GetFullPath(FilePath));
            OnPropertyChanged(nameof(IsFileOpened));
        }

        [RelayCommand]
        public void CloseFile()
        {
            if (!IsFileOpened)
            {
                return;
            }
            if (this.UpdatingTask != null)
            {
                Debug.Assert(this.UpdatingTaskCancellationTokenSource != null);
                this.UpdatingTaskCancellationTokenSource.Cancel();
                this.UpdatingTaskCancellationTokenSource = null;
                this.UpdatingTask.Wait();
                this.UpdatingTask.Dispose();
                this.UpdatingTask = null;
            }
            if (this.Reader != null)
            {
                this.Reader.Dispose();
                this.Reader = null;
            }
            // Drain pending entries and stop the flush timer
            FlushPendingEntries();
            FlushTimer?.Stop();
            IsFlushScheduled = false;
            lock (PendingEntriesLock)
            {
                PendingEntries.Clear();
            }
            if (Entries.Count != 0)
            {
                ClearEntries();
            }
            this.FilePath = null;
            OnPropertyChanged(nameof(IsFileOpened));
        }

        private void PushBackEntriesInMainThread(List<Entry> Entries)
        {
            using (
                var _ = Entries.Count >= CoreModule.Instance.BatchOperationThreshold
                    ? new EntryObservableCollection.BatchOperation(this.Entries)
                    : null
            )
            {
                for (int I = 0; I < Entries.Count; I++)
                {
                    Entries[I].Line = (uint)this.Entries.Count + 1;
                    this.Entries.Add(Entries[I]);
                }
            }
            OnPropertyChanged(nameof(this.Entries));
            EntriesAdded?.Invoke(Entries);
            EntriesRefreshed?.Invoke();
        }

        private void PopBackEntryInMainThread()
        {
            Entries.RemoveAt(Entries.Count - 1);
            OnPropertyChanged(nameof(Entries));
            EntriesRefreshed?.Invoke();
        }

        protected void ClearEntriesInMainThread()
        {
            Entries.Clear();
            OnPropertyChanged(nameof(this.Entries));
            EntriesCleared?.Invoke();
            EntriesRefreshed?.Invoke();
        }

        #region Coalesce / Backpressure

        /// <summary>
        /// Enqueues entries into the pending buffer (called on background thread).
        /// A flush timer periodically drains the buffer to the UI in bulk,
        /// avoiding a backlog of Dispatcher.InvokeAsync operations when the
        /// window is backgrounded for a long time.
        /// </summary>
        private void PushBackEntries(List<Entry> Entries)
        {
            if (Entries.Count == 0)
            {
                return;
            }

            // EntryCreated callbacks still run on the background thread (non-blocking to UI)
            foreach (var Entry in Entries)
            {
                EntryCreated?.Invoke(Entry);
            }

            lock (PendingEntriesLock)
            {
                PendingEntries.AddRange(Entries);
            }

            ScheduleFlush();
        }

        /// <summary>
        /// Called by EntriesWindow to notify whether the window is active.
        /// Increases flush frequency when active, decreases when inactive to
        /// reduce UI thread pressure. Immediately flushes all pending entries
        /// when the window is re-activated.
        /// </summary>
        public void SetWindowActive(bool isActive)
        {
            if (FlushTimer != null)
            {
                FlushTimer.Interval = TimeSpan.FromMilliseconds(
                    isActive ? CoreModule.Instance.FlushIntervalActive : CoreModule.Instance.FlushIntervalInactive);
            }
            if (isActive)
            {
                // Window re-activated, immediately flush all pending entries
                FlushPendingEntries();
            }
        }

        private void ScheduleFlush()
        {
            if (IsFlushScheduled)
            {
                return;
            }
            IsFlushScheduled = true;
            if (FlushTimer != null && !FlushTimer.IsEnabled)
            {
                // Use the active-interval from CoreModule as the initial rate;
                // SetWindowActive will adjust it when the window state changes.
                FlushTimer.Interval = TimeSpan.FromMilliseconds(CoreModule.Instance.FlushIntervalActive);
                FlushTimer.Start();
            }
        }

        private void OnFlushTimerTick(object? sender, EventArgs e)
        {
            FlushPendingEntries();

            lock (PendingEntriesLock)
            {
                if (PendingEntries.Count == 0)
                {
                    FlushTimer?.Stop();
                    IsFlushScheduled = false;
                }
            }
        }

        /// <summary>
        /// Drains all pending entries as a single bulk push on the UI thread
        /// at Background priority so rendering and input are not blocked.
        /// </summary>
        private void FlushPendingEntries()
        {
            List<Entry> entriesToPush;
            lock (PendingEntriesLock)
            {
                if (PendingEntries.Count == 0)
                {
                    return;
                }
                entriesToPush = new List<Entry>(PendingEntries);
                PendingEntries.Clear();
            }

            Application.Current?.Dispatcher.InvokeAsync(
                () => PushBackEntriesInMainThread(entriesToPush),
                DispatcherPriority.Background);
        }

        #endregion

        private void PopBackEntry()
        {
            _ = Application.Current?.Dispatcher.InvokeAsync(() => PopBackEntryInMainThread());
        }

        private void ClearEntries()
        {
            // Discard all pending entries so stale entries are not pushed after clear
            lock (PendingEntriesLock)
            {
                PendingEntries.Clear();
            }
            FlushTimer?.Stop();
            IsFlushScheduled = false;
            _ = Application.Current?.Dispatcher.InvokeAsync(() => ClearEntriesInMainThread());
        }

        private async void Update(CancellationToken UpdatingTaskCancellationToken)
        {
            Debug.Assert(this.Reader != null);
            Memory<char> Buffer = new(new char[this.BufferSize]);
            long TotalLength = 0;
            EntryGenerator Generator = new();
            while (!UpdatingTaskCancellationToken.IsCancellationRequested)
            {
                if (TotalLength > this.Reader.BaseStream.Length)
                {
                    ClearEntries();
                    Generator.Clear();
                    this.Reader.BaseStream.Position = 0;
                }
                TotalLength = this.Reader.BaseStream.Length;
                if (!Reader.EndOfStream)
                {
                    int Length = await Reader.ReadAsync(Buffer);
                    if (!Generator.LastFinished)
                    {
                        PopBackEntry();
                    }
                    PushBackEntries(Generator.Push(Buffer, Length));
                }
                else
                {
                    await Task.Delay(Interval);
                }
                await TryGenerateDebugEntries();
            }
        }

        private object IsDebugEntriesRequestedLock = new();
        private bool IsDebugEntriesRequested = false;

        [RelayCommand]
        public void RequestDebugEntries()
        {
            lock(IsDebugEntriesRequestedLock)
            {
                if (!IsDebugEntriesRequested)
                {
                    IsDebugEntriesRequested = true;
                }
            }
        }

        private async Task TryGenerateDebugEntries()
        {
            lock (IsDebugEntriesRequestedLock)
            {
                if (!IsDebugEntriesRequested)
                {
                    return;
                }
                IsDebugEntriesRequested = false;
            }
            int FrameRate = 60;
            int Seconds = 20;
            int EntryCountPerOneFrame = 10;
            for (int FrameIndex = 0; FrameIndex < FrameRate * Seconds; FrameIndex++)
            {
                List<Entry> Entries = new();
                for (int Index = 0; Index < EntryCountPerOneFrame; Index++)
                {
                    var Text = $"{DateTime.Now.ToString("[yyyy.MM.dd-hh.mm.ss:fff][0] ") + "{A=" + Index + "}"}";
                    Entries.Add(new Entry(Text));
                }
                PushBackEntries(Entries);
                await Task.Delay(1000 / FrameRate);
            }
        }
    }
}
