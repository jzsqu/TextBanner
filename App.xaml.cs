using System.IO;
using System.Windows;
using Microsoft.Win32;
using TextBanner.Services;
using TextBanner.UI;

namespace TextBanner;

public partial class App : System.Windows.Application
{
    private Mutex _mutex;
    private AppServices _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, ex) =>
        {
            Log(ex.Exception);
            ex.Handled = true;
        };
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            Log(ex.ExceptionObject as Exception);
        };
        TaskScheduler.UnobservedTaskException += (s, ex) =>
        {
            Log(ex.Exception);
            ex.SetObserved();
        };

        try
        {
            _mutex = new Mutex(true, @"Global\TextBanner_SingleInstance", out bool createdNew);

            bool minimized = e.Args.Any(a =>
                a.Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("-m", StringComparison.OrdinalIgnoreCase));

            _services = new AppServices(minimized);
            ThemeManager.Apply(_services.Config.General.Theme); // 在创建任何窗口前先应用主题
            Loc.Lang = _services.Config.General.Language;

            if (!createdNew)
            {
                System.Windows.MessageBox.Show(Loc.Get("MsgRunning"), Loc.Get("AppTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
                _services.Dispose();
                _services = null;
                Shutdown();
                return;
            }

            _services.Start();

            if (e.Args.Any(a => a.Equals("--test", StringComparison.OrdinalIgnoreCase)))
                _services.Banner.Show(Loc.Get("TestContent"), 6, "large", Loc.Get("TestTitle"));
        }
        catch (Exception ex)
        {
            Log(ex);
            System.Windows.MessageBox.Show(Loc.Get("MsgStartupFailed") + ex.Message, Loc.Get("AppTitle"),
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _services?.Dispose(); } catch { }
        try { _mutex?.ReleaseMutex(); } catch { }
        base.OnExit(e);
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            Dispatcher.BeginInvoke(() => ThemeManager.ReapplyIfAuto());
        }
    }

    private static void Log(Exception ex)
    {
        if (ex == null) return;
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TextBanner");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "error.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
        }
        catch { }
    }
}
