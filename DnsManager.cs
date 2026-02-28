using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using Microsoft.Win32;

namespace obhod;

public static class DnsManager
{
    public static void EnableSecureDns(Action<string, LogLevel> log)
    {
        try
        {
            SetSystemDoh(log);
            FlushDns(log);
            log("Secure DNS (DoH) активирован.", LogLevel.Success);
        }
        catch (Exception ex)
        {
            log($"Не удалось настроить DNS: {ex.Message}", LogLevel.Warn);
        }
    }

    public static void DisableSecureDns(Action<string, LogLevel> log)
    {
        try
        {
            ResetSystemDns(log);
            FlushDns(log);
            log("DNS восстановлен по умолчанию.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            log($"Ошибка сброса DNS: {ex.Message}", LogLevel.Warn);
        }
    }

    private static void SetSystemDoh(Action<string, LogLevel> log)
    {
        string[] adapters = { "Ethernet", "Wi-Fi", "Беспроводная сеть", "Подключение по локальной сети" };

        bool anySet = false;

        foreach (var adapter in adapters)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"interface ipv4 show interface \"{adapter}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                var check = Process.Start(psi);
                check?.WaitForExit(3000);
                if (check == null || check.ExitCode != 0) continue;

                RunNetsh($"interface ipv4 set dnsservers \"{adapter}\" static 8.8.8.8 primary", log);
                RunNetsh($"interface ipv4 add dnsservers \"{adapter}\" 1.1.1.1 index=2", log);
                RunNetsh($"interface ipv6 set dnsservers \"{adapter}\" static 2001:4860:4860::8888 primary", log);
                RunNetsh($"interface ipv6 add dnsservers \"{adapter}\" 2606:4700:4700::1111 index=2", log);

                anySet = true;
                log($"DNS настроен для: {adapter}", LogLevel.Info);
            }
            catch { }
        }

        if (!anySet)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "interface show interface",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                var proc = Process.Start(psi);
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(3000);
                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains("Connected") || line.Contains("Подключен"))
                        {
                            string name = line.Trim();
                            int lastSpace = name.LastIndexOf("  ");
                            if (lastSpace > 0)
                            {
                                name = name[(lastSpace + 2)..].Trim();
                                if (!string.IsNullOrEmpty(name) && name != "Name" && name != "Имя")
                                {
                                    try
                                    {
                                        RunNetsh($"interface ipv4 set dnsservers \"{name}\" static 8.8.8.8 primary", log);
                                        RunNetsh($"interface ipv4 add dnsservers \"{name}\" 1.1.1.1 index=2", log);
                                        anySet = true;
                                        log($"DNS настроен для: {name}", LogLevel.Info);
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        if (!anySet)
        {
            log("Не найден активный сетевой адаптер. Настройте DNS вручную: 8.8.8.8", LogLevel.Warn);
        }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", true);
            if (key != null)
            {
                key.SetValue("EnableAutoDoh", 2, RegistryValueKind.DWord);
            }
        }
        catch { }
    }

    private static void ResetSystemDns(Action<string, LogLevel> log)
    {
        string[] adapters = { "Ethernet", "Wi-Fi", "Беспроводная сеть", "Подключение по локальной сети" };

        foreach (var adapter in adapters)
        {
            try
            {
                RunNetsh($"interface ipv4 set dnsservers \"{adapter}\" dhcp", log);
                RunNetsh($"interface ipv6 set dnsservers \"{adapter}\" dhcp", log);
            }
            catch { }
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "interface show interface",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            var proc = Process.Start(psi);
            if (proc != null)
            {
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(3000);
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("Connected") || line.Contains("Подключен"))
                    {
                        string name = line.Trim();
                        int lastSpace = name.LastIndexOf("  ");
                        if (lastSpace > 0)
                        {
                            name = name[(lastSpace + 2)..].Trim();
                            if (!string.IsNullOrEmpty(name) && name != "Name" && name != "Имя")
                            {
                                try
                                {
                                    RunNetsh($"interface ipv4 set dnsservers \"{name}\" dhcp", log);
                                    RunNetsh($"interface ipv6 set dnsservers \"{name}\" dhcp", log);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
        }
        catch { }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", true);
            if (key != null)
            {
                // 0 = отключено или по умолчанию
                key.SetValue("EnableAutoDoh", 0, RegistryValueKind.DWord);
            }
        }
        catch { }
    }

    private static void RunNetsh(string args, Action<string, LogLevel> log)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = args,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var proc = Process.Start(psi);
        proc?.WaitForExit(5000);
    }

    public static void FlushDns(Action<string, LogLevel> log)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ipconfig",
                Arguments = "/flushdns",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
        catch { }
    }
}
