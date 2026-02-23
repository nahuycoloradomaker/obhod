using System.Diagnostics;
using System.IO;

namespace obhod;

public class ProcessManager
{
    private readonly Action<string, LogLevel> _log;
    private Process? _proc;
    private readonly object _lock = new();

    public ProcessManager(Action<string, LogLevel> log)
    {
        _log = log;
    }

    public Task<bool> StartProcessAsync(string exe, string args, string name)
    {
        return Task.Run(async () =>
        {
            StopAll();

            try
            {
                if (!File.Exists(exe))
                {
                    _log($"Файл не найден: {exe}", LogLevel.Error);
                    return false;
                }

                var psi = new ProcessStartInfo
                {
                    FileName               = exe,
                    Arguments              = args,
                    WorkingDirectory       = Path.GetDirectoryName(exe)!,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                };

                var proc = new Process { StartInfo = psi };

                proc.OutputDataReceived += (_, _) => { };
                proc.ErrorDataReceived += (_, _) => { };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                await Task.Delay(1500);

                try
                {
                    if (proc.HasExited)
                    {
                        int code = -1;
                        try { code = proc.ExitCode; } catch { }
                        _log($"Движок упал при старте (код {code})", LogLevel.Error);
                        _log("Возможно антивирус заблокировал файлы WinDivert", LogLevel.Warn);
                        try { proc.Dispose(); } catch { }
                        return false;
                    }
                }
                catch
                {
                    _log("Не удалось проверить статус движка", LogLevel.Error);
                    try { proc.Dispose(); } catch { }
                    return false;
                }

                lock (_lock)
                {
                    _proc = proc;
                }

                _log($"Движок запущен, PID {proc.Id}", LogLevel.Success);
                return true;
            }
            catch (Exception ex)
            {
                _log($"Ошибка запуска: {ex.Message}", LogLevel.Error);
                return false;
            }
        });
    }

    public void StopAll()
    {
        Process? proc;
        lock (_lock)
        {
            proc = _proc;
            _proc = null;
        }

        if (proc == null) return;

        try
        {
            if (!proc.HasExited)
            {
                proc.Kill(true);
                proc.WaitForExit(3000);
            }
        }
        catch { }

        try { proc.Dispose(); } catch { }
    }

    public Task StopAllAsync()
    {
        return Task.Run(StopAll);
    }
}
