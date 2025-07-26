using LogGenius.Core;
using Serilog;
using System.Windows;

namespace LogGenius
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("LogGenius.log", Serilog.Events.LogEventLevel.Debug)
                .WriteTo.Console()
                .CreateLogger();
            DispatcherUnhandledException += (Sender, EventArgs) =>
            {
                Log.Fatal(EventArgs.Exception.ToString());
                EventArgs.Handled = true;
                Application.Current.Shutdown();
            };
            AppDomain.CurrentDomain.UnhandledException += (Sender, EventArgs) =>
            {
                if (EventArgs.ExceptionObject is Exception Exception)
                {
                    Log.Fatal(Exception.ToString());
                }
                Application.Current.Shutdown();
            };
            try
            {
                Log.Information("Starting LogGenius application...");
                base.OnStartup(e);
                Manager.Instance.RegisterFromAssemblies(new Uri(AppDomain.CurrentDomain.BaseDirectory));
                Manager.Instance.StartUp();
                if (e.Args.Length != 0)
                {
                    Log.Information($"Open file {e.Args[0]}");
                    Manager.Instance.Session.OpenFile(e.Args[0]);
                }
            }
            catch (Exception Exception)
            {
                Log.Fatal(Exception.ToString());
                Environment.Exit(0);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Manager.Instance.SaveSettings();
            base.OnExit(e);
        }
    }
}
