using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace obhod
{
    public class UpdateManager
    {
        private const string GITHUB_USER = "nahuycoloradomaker";
        private const string GITHUB_REPO = "obhod";
        private const string API_URL = $"https://api.github.com/repos/{GITHUB_USER}/{GITHUB_REPO}/releases/latest";

        public static async Task CheckForUpdatesAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "obhod-updater");

                var release = await client.GetFromJsonAsync<GithubRelease>(API_URL);
                if (release == null || string.IsNullOrEmpty(release.TagName)) return;

                Version latestVersion = Version.Parse(release.TagName.TrimStart('v'));
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

                if (latestVersion > currentVersion)
                {
                    var result = MessageBox.Show(
                        $"Р”РѕСЃС‚СѓРїРЅР° РЅРѕРІР°СЏ РІРµСЂСЃРёСЏ {release.TagName}!\n\nРҐРѕС‚РёС‚Рµ РѕР±РЅРѕРІРёС‚СЊСЃСЏ СЃРµР№С‡Р°СЃ? РџСЂРёР»РѕР¶РµРЅРёРµ Р±СѓРґРµС‚ РїРµСЂРµР·Р°РїСѓС‰РµРЅРѕ.",
                        "РћР±РЅРѕРІР»РµРЅРёРµ",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        foreach (var asset in release.Assets)
                        {
                            if (asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                await PerformUpdate(asset.BrowserDownloadUrl, asset.Name);
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static async Task PerformUpdate(string url, string fileName)
        {
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), fileName);
                
                using (var client = new HttpClient())
                {
                    var data = await client.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(tempPath, data);
                }

                string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(currentExe)) return;

                string batchPath = Path.Combine(Path.GetTempPath(), "update_obhod.bat");
                string batchContent = $@"
@echo off
timeout /t 1 /nobreak > nul
:loop
del ""{currentExe}""
if exist ""{currentExe}"" (
    timeout /t 1 /nobreak > nul
    goto loop
)
move /y ""{tempPath}"" ""{currentExe}""
start """" ""{currentExe}""
del ""%~f0""
";
                await File.WriteAllTextAsync(batchPath, batchContent);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batchPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"РћС€РёР±РєР° РїСЂРё РѕР±РЅРѕРІР»РµРЅРёРё: {ex.Message}", "РћС€РёР±РєР°", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private class GithubRelease
        {
            public string TagName { get; set; } = "";
            public GithubAsset[] Assets { get; set; } = Array.Empty<GithubAsset>();
        }

        private class GithubAsset
        {
            public string Name { get; set; } = "";
            public string BrowserDownloadUrl { get; set; } = "";
        }
    }
}
