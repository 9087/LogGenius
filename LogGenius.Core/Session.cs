using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Serilog;

namespace LogGenius.Core
{
    public partial class Session : ObservableObject
    {
        [ObservableProperty]
        protected ObservableCollection<Entry> _Entries = new();

        public int Interval => CoreModule.Instance.UpdateInterval;

        public int BufferSize => CoreModule.Instance.UpdateBufferSize;

        private StreamReader? Reader { get; set; } = null;

        public Action? EntriesRefreshed { get; set; }

        public Action<List<Entry>>? EntriesAdded { get; set; }

        public Action? EntriesCleared { get; set; }

        private Task? UpdatingTask = null;

        private CancellationTokenSource? UpdatingTaskCancellationTokenSource;

        [ObservableProperty]
        private string? _FilePath = null;

        public Session()
        {
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
            if (Entries.Count != 0)
            {
                ClearEntries();
            }
            this.FilePath = null;
            OnPropertyChanged(nameof(IsFileOpened));
        }

        private void PushBackEntriesInMainThread(List<Entry> Entries)
        {
            for (int I = 0; I < Entries.Count; I++)
            {
                Entries[I].Line = (uint)this.Entries.Count;
                this.Entries.Add(Entries[I]);
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

        private void PushBackEntries(List<Entry> Entries)
        {
            _ = Application.Current.Dispatcher.InvokeAsync(() => PushBackEntriesInMainThread(Entries));
        }

        private void PopBackEntry()
        {
            _ = Application.Current?.Dispatcher.InvokeAsync(() => PopBackEntryInMainThread());
        }

        private void ClearEntries()
        {
            _ = Application.Current.Dispatcher.InvokeAsync(() => ClearEntriesInMainThread());
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
            }
        }
    }
}
