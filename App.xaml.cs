using System.Windows;
using System.Security.Principal;

namespace obhod;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, _) => { };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            args.SetObserved();
        };

        if (!IsAdmin())
        {
            AdminElevation.RestartAsAdmin();
            Shutdown();
            return;
        }

        // Показываем неоновое интро
        _ = ShowSplashAndStart();
    }

    private async Task ShowSplashAndStart()
    {
        // Отключаем автоматическое закрытие при закрытии первого окна
        Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var splash = new SplashWindow();
        splash.Show();
        
        // Время на "полюбоваться" неонкой
        await Task.Delay(3500);
        
        var main = new MainWindow();
        Current.MainWindow = main;
        main.Show();
        
        splash.Close();

        // Возвращаем нормальный режим: выход при закрытии главного окна
        Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    private static bool IsAdmin()
    {
        using var id = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
