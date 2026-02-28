using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Diagnostics;

namespace obhod;

public partial class MainWindow : Window
{
    private readonly BypassEngine _engine;
    private bool _running;
    private bool _closing;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        _engine = new BypassEngine(Log);
        Closing += OnClose;
        UpdatePresetLabel();
        Log("obhod v2.0 готов к работе.", LogLevel.Info);
        
        _ = UpdateManager.CheckForUpdatesAsync();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        try { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); } catch { }
    }

    private void Btn_Close(object sender, RoutedEventArgs e) => Close();
    private void Btn_Min(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private async void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (_closing || _busy) return;
        _busy = true;
        MainToggle.IsEnabled = false;

        try
        {
            if (!_running)
                await DoStart();
            else
                await DoStop();
        }
        catch
        {
            _running = false;
            try { MainToggle.IsChecked = false; } catch { }
            SetStatus(false, "Ошибка", "попробуйте снова");
        }

        _busy = false;
        if (!_closing)
        {
            try { MainToggle.IsEnabled = true; } catch { }
        }
    }

    private async Task DoStart()
    {
        var presets = ActivePresets();
        if (presets.Count == 0)
        {
            Log("Выберите хотя бы один сервис.", LogLevel.Warn);
            MainToggle.IsChecked = false;
            return;
        }

        SetStatus(false, "Запуск...", "подождите");

        bool ok = false;
        try
        {
            ok = await _engine.StartAsync(presets);
        }
        catch (Exception ex)
        {
            Log($"Ошибка: {ex.Message}", LogLevel.Error);
        }

        if (ok)
        {
            _running = true;
            MainToggle.IsChecked = true;
            string Fmt(string p) => p switch { "ukraine" => "UA→RU", _ => char.ToUpper(p[0]) + p[1..] };
            SetStatus(true, "Активен", string.Join(" · ", presets.Select(Fmt)));
        }
        else
        {
            _running = false;
            MainToggle.IsChecked = false;
            SetStatus(false, "Ошибка", "смотрите логи");
        }
    }

    private async Task DoStop()
    {
        SetStatus(false, "Отключение...", "");
        try
        {
            await _engine.StopAsync();
        }
        catch { }
        _running = false;
        MainToggle.IsChecked = false;
        SetStatus(false, "Отключён", "Нажмите переключатель");
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        UpdatePresetLabel();
        if (_running && !_busy)
            _ = RestartSafe();
    }

    private async Task RestartSafe()
    {
        if (_busy) return;
        _busy = true;
        MainToggle.IsEnabled = false;
        try
        {
            await DoStop();
            await Task.Delay(500);
            await DoStart();
        }
        catch { }
        _busy = false;
        if (!_closing)
            MainToggle.IsEnabled = true;
    }

    private List<string> ActivePresets()
    {
        var list = new List<string>();
        try
        {
            if (P_Discord.IsChecked == true) list.Add("discord");
            if (P_YouTube.IsChecked == true) list.Add("youtube");
            if (P_Roblox.IsChecked == true) list.Add("roblox");
            if (P_HTTPS.IsChecked == true) list.Add("https");
            if (P_QUIC.IsChecked == true) list.Add("quic");
            if (P_Ukraine.IsChecked == true) list.Add("ukraine");
        }
        catch { }
        return list;
    }

    private void UpdatePresetLabel()
    {
        try
        {
            var active = ActivePresets();
            string FormatPreset(string p) => p switch
            {
                "ukraine" => "UA→RU",
                _ => char.ToUpper(p[0]) + p[1..]
            };
            TxtPresetList.Text = active.Count > 0
                ? string.Join(" · ", active.Select(FormatPreset))
                : "ничего не выбрано";
        }
        catch { }
    }

    private void SetStatus(bool active, string main, string sub)
    {
        try
        {
            Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    TxtStatus.Text = main;
                    TxtSub.Text = sub;

                    if (active)
                    {
                        Anim(StatusTextColor, Color.FromRgb(130, 220, 160));
                        Anim(DotColor, Color.FromRgb(50, 200, 100));
                        Anim(DotGlow, Color.FromRgb(50, 200, 100));
                        DotGlow.BlurRadius = 14;
                        DotGlow.Opacity = 1.0;
                        Anim(StatusBorder, Color.FromArgb(70, 50, 200, 100));
                        Anim(StatusBg, Color.FromArgb(18, 50, 200, 100));
                    }
                    else
                    {
                        Anim(StatusTextColor, Color.FromRgb(120, 120, 140));
                        Anim(DotColor, Color.FromRgb(48, 48, 64));
                        Anim(DotGlow, Color.FromRgb(48, 48, 64));
                        DotGlow.BlurRadius = 0;
                        DotGlow.Opacity = 0;
                        Anim(StatusBorder, Color.FromArgb(30, 255, 255, 255));
                        Anim(StatusBg, Color.FromArgb(18, 255, 255, 255));
                    }
                }
                catch { }
            });
        }
        catch { }
    }

    private static void Anim(SolidColorBrush b, Color to)
    {
        try
        {
            b.BeginAnimation(SolidColorBrush.ColorProperty,
                new ColorAnimation(to, TimeSpan.FromMilliseconds(350)));
        }
        catch { }
    }

    private static void Anim(DropShadowEffect e, Color to)
    {
        try
        {
            e.BeginAnimation(DropShadowEffect.ColorProperty,
                new ColorAnimation(to, TimeSpan.FromMilliseconds(350)));
        }
        catch { }
    }

    public void Log(string msg, LogLevel level = LogLevel.Info)
    {
        try
        {
            Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    string t = DateTime.Now.ToString("HH:mm:ss");
                    string icon = level switch
                    {
                        LogLevel.Success => "✓",
                        LogLevel.Error   => "✗",
                        LogLevel.Warn    => "!",
                        _                => "·"
                    };
                    LogTB.Text += $"[{t}] {icon} {msg}\n";
                    LogSV.ScrollToEnd();
                }
                catch { }
            });
        }
        catch { }
    }

    private void Btn_ClearLogs(object sender, RoutedEventArgs e)
    {
        try { LogTB.Text = string.Empty; } catch { }
    }

    private async void Btn_Uninstall(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CustomDialog.ShowDialog("Удаление", "Удалить приложение и сбросить настройки?", true))
            {
                if (_running) await DoStop();
                _engine.Dispose();

                try
                {
                    var psi = new ProcessStartInfo("sc.exe", "stop WinDivert") { CreateNoWindow = true, UseShellExecute = false };
                    Process.Start(psi)?.WaitForExit(3000);
                    psi.Arguments = "delete WinDivert";
                    Process.Start(psi)?.WaitForExit(3000);
                }
                catch { }

                CustomDialog.ShowDialog("obhod", "Готово.");
                Application.Current.Shutdown();
            }
        }
        catch { }
    }

    private async void OnClose(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_closing) return;
        _closing = true;

        if (_running)
        {
            e.Cancel = true;
            try { await DoStop(); } catch { }
            try { _engine.Dispose(); } catch { }
            try { Application.Current.Shutdown(); } catch { }
        }
        else
        {
            try { _engine.Dispose(); } catch { }
        }
    }
}

public enum LogLevel { Info, Success, Error, Warn }
