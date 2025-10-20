using System.Windows;
using System.Windows.Threading;

namespace LogGenius.Core
{
    public class Debouncer
    {
        private Action Callback;
        private DispatcherOperation? DispatcherOperation;
        private DispatcherPriority Priority;

        protected Debouncer(Action Callback, DispatcherPriority Priority = DispatcherPriority.Background)
        {
            this.Callback = Callback;
            this.Priority = Priority;
        }

        public static Debouncer Create(Action Callback)
        {
            return new Debouncer(Callback);
        }

        public void Schedule()
        {
            if (DispatcherOperation != null)
            {
                return;
            }
            DispatcherOperation = Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    Callback();
                    DispatcherOperation = null;
                },
                Priority
            );
        }

        public void ExecuteImmediately()
        {
            if (DispatcherOperation != null)
            {
                switch (DispatcherOperation.Status)
                {
                    case DispatcherOperationStatus.Completed:
                    case DispatcherOperationStatus.Executing:
                        DispatcherOperation = null;
                        return;
                    case DispatcherOperationStatus.Pending:
                    case DispatcherOperationStatus.Aborted:
                        DispatcherOperation = null;
                        break;
                }
            }
            Callback();
        }
    }
}
