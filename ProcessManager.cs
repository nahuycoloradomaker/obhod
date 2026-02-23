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
                    _log($"Р¤Р°Р№Р» РЅРµ РЅР°Р№РґРµРЅ: {exe}", LogLevel.Error);
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
                        _log($"Р”РІРёР¶РѕРє СѓРїР°Р» РїСЂРё СЃС‚Р°СЂС‚Рµ (РєРѕРґ {code})", LogLevel.Error);
                        _log("Р’РѕР·РјРѕР¶РЅРѕ Р°РЅС‚РёРІРёСЂСѓСЃ Р·Р°Р±Р»РѕРєРёСЂРѕРІР°Р» С„Р°Р№Р»С‹ WinDivert", LogLevel.Warn);
                        try { proc.Dispose(); } catch { }
                        return false;
                    }
                }
                catch
                {
                    _log("РќРµ СѓРґР°Р»РѕСЃСЊ РїСЂРѕРІРµСЂРёС‚СЊ СЃС‚Р°С‚СѓСЃ РґРІРёР¶РєР°", LogLevel.Error);
                    try { proc.Dispose(); } catch { }
                    return false;
                }

                lock (_lock)
                {
                    _proc = proc;
                }

                _log($"Р”РІРёР¶РѕРє Р·Р°РїСѓС‰РµРЅ, PID {proc.Id}", LogLevel.Success);
                return true;
            }
            catch (Exception ex)
            {
                _log($"РћС€РёР±РєР° Р·Р°РїСѓСЃРєР°: {ex.Message}", LogLevel.Error);
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
