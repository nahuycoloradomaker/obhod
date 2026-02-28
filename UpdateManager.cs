using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
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
                client.Timeout = TimeSpan.FromSeconds(10);

                var release = await client.GetFromJsonAsync<GithubRelease>(API_URL);
                if (release == null || string.IsNullOrEmpty(release.TagName)) return;

                string tagClean = release.TagName.TrimStart('v');
                if (!Version.TryParse(tagClean, out var latestVersion)) return;

                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

                if (latestVersion > currentVersion)
                {
                    bool result = CustomDialog.ShowDialog(
                        "Обновление obhod",
                        $"Доступна новая версия {release.TagName}!\n\nХотите обновиться сейчас? Приложение будет перезапущено.",
                        true);

                    if (result)
                    {
                        foreach (var asset in release.Assets)
                        {
                            if (asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                await PerformUpdate(asset.DownloadUrl, asset.Name);
                                return;
                            }
                        }
                        CustomDialog.ShowDialog("Ошибка", "exe не найден в релизе.");
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
                    client.Timeout = TimeSpan.FromMinutes(5);
                    var data = await client.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(tempPath, data);
                }

                string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(currentExe)) return;

                string batchPath = Path.Combine(Path.GetTempPath(), "update_obhod.bat");
                string batchContent = $@"
@echo off
timeout /t 2 /nobreak > nul
:loop
taskkill /f /im obhod.exe > nul 2>&1
timeout /t 1 /nobreak > nul
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
                CustomDialog.ShowDialog("Ошибка", $"Ошибка при обновлении: {ex.Message}");
            }
        }

        public class GithubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = "";

            [JsonPropertyName("assets")]
            public GithubAsset[] Assets { get; set; } = Array.Empty<GithubAsset>();
        }

        public class GithubAsset
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("browser_download_url")]
            public string DownloadUrl { get; set; } = "";
        }
    }
}
