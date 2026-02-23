using System.Diagnostics;

namespace obhod;

public static class AdminElevation
{
    public static void RestartAsAdmin()
    {
        try
        {
            string? exe = Environment.ProcessPath
                          ?? Process.GetCurrentProcess().MainModule?.FileName;

            Process.Start(new ProcessStartInfo
            {
                FileName        = exe,
                UseShellExecute = true,
                Verb            = "runas",
                Arguments       = string.Join(" ", Environment.GetCommandLineArgs().Skip(1))
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Нужны права администратора.\n\n{ex.Message}",
                "obhod",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }
}
