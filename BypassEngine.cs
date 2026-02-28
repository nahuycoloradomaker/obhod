using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace obhod;

public class BypassEngine : IDisposable
{
    private readonly Action<string, LogLevel> _log;
    private readonly ProcessManager _procManager;
    private string? _tempDir;
    private bool _disposed;

    private const string RES_WINWS    = "obhod.Resources.obhod_core.exe";
    private const string RES_CYGWIN   = "obhod.Resources.cygwin1.dll";
    private const string RES_DIVERT   = "obhod.Resources.WinDivert.dll";
    private const string RES_DIVERT64 = "obhod.Resources.WinDivert64.sys";
    private const string RES_BIN_QUIC = "obhod.Resources.quic_initial_www_google_com.bin";
    private const string RES_BIN_TLSS = "obhod.Resources.tls_clienthello_www_google_com.bin";
    private const string RES_BIN_TLS4 = "obhod.Resources.tls_clienthello_4pda_to.bin";

    private static readonly string DiscordDomains = string.Join(",",
        "discord.com", "discordapp.com", "discord.gg", "discordapp.net",
        "cdn.discordapp.com", "media.discordapp.net", "images.discordapp.net",
        "gateway.discord.gg", "status.discord.com", "discordcdn.com", "discord.media",
        "discord.app", "discord.co", "discord.design", "discord.dev",
        "discord.gift", "discord.gifts", "discord.new", "discord.store",
        "discord-activities.com", "discordactivities.com", "discordmerch.com",
        "discordpartygames.com", "discordsays.com", "discordsez.com", "discordstatus.com",
        "dis.gd", "discord-attachments-uploads-prd.storage.googleapis.com",
        "stable.dl2.discordapp.net"
    );

    private static readonly string YouTubeDomains = string.Join(",",
        "youtube.com", "youtu.be", "googlevideo.com", "ytimg.com",
        "i.ytimg.com", "yt3.ggpht.com", "yt4.ggpht.com",
        "yt3.googleusercontent.com", "youtubei.googleapis.com",
        "youtubeembeddedplayer.googleapis.com", "youtube-nocookie.com",
        "youtube-ui.l.google.com", "wide-youtube.l.google.com",
        "youtubekids.com", "jnn-pa.googleapis.com",
        "yt-video-upload.l.google.com", "ytimg.l.google.com",
        "googleapis.com", "gvt1.com"
    );

    private static readonly string RobloxDomains = string.Join(",",
        "roblox.com", "rbxcdn.com", "images.rbxcdn.com", "static.rbxcdn.com",
        "assetdelivery.roblox.com", "roblox-player.com", "clientsettings.roblox.com",
        "setup.roblox.com", "rbxtrk.com", "rbxstatic.com", "robloxlabs.com", "rbx.com",
        "akamaihd.net", "akamaized.net", "fastly.net",
        "apis.roblox.com", "auth.roblox.com", "catalog.roblox.com",
        "economy.roblox.com", "games.roblox.com", "realtime.roblox.com",
        "thumbnails.roblox.com", "users.roblox.com", "www.roblox.com",
        "web.roblox.com", "avatar.roblox.com", "groups.roblox.com",
        "inventory.roblox.com", "presence.roblox.com", "develop.roblox.com",
        "badges.roblox.com", "locale.roblox.com", "accountsettings.roblox.com",
        "notifications.roblox.com", "premiumfeatures.roblox.com",
        "trades.roblox.com", "friends.roblox.com",
        "chat.roblox.com", "followings.roblox.com",
        "metrics.roblox.com", "midas.roblox.com", "voice.roblox.com",
        "gamejoin.roblox.com", "assetgame.roblox.com"
    );

    private static readonly string GeneralDomains = string.Join(",",
        "cloudflare.com", "cdnjs.cloudflare.com", "akamaihd.net", "akamaized.net", "fastly.net",
        "cloudflare-ech.com", "encryptedsni.com", "cloudflareaccess.com",
        "cloudflareapps.com", "cloudflarebolt.com", "cloudflareclient.com",
        "cloudflareinsights.com", "cloudflareok.com", "cloudflarepartners.com",
        "cloudflareportal.com", "cloudflarepreview.com", "cloudflareresolve.com",
        "cloudflaressl.com", "cloudflarestatus.com", "cloudflarestorage.com",
        "cloudflarestream.com", "cloudflaretest.com",
        "frankerfacez.com", "ffzap.com", "betterttv.net",
        "7tv.app", "7tv.io", "localizeapi.com"
    );

    private static readonly string UkraineDomains = string.Join(",",
        "vk.com", "vk.me", "vk.cc", "vk.link", "vkontakte.ru", "vk.ru",
        "userapi.com", "vk-cdn.net", "vkuseraudio.net", "vkuservideo.net",
        "mail.ru", "list.ru", "inbox.ru", "bk.ru", "internet.ru",
        "mycdn.me", "imgsmail.ru", "mradx.net",
        "ok.ru", "odnoklassniki.ru", "odkl.ru",
        "yandex.ru", "yandex.com", "yandex.net", "yastatic.net",
        "yandex.ua", "ya.ru", "yandex.by",
        "kinopoisk.ru", "kinopoisk.hd",
        "rutube.ru",
        "1tv.ru", "ntv.ru", "ren.tv", "sts.tv", "vesti.ru",
        "rbc.ru", "ria.ru", "tass.ru", "lenta.ru", "gazeta.ru", "iz.ru",
        "kommersant.ru", "vedomosti.ru", "fontanka.ru",
        "sberbank.ru", "online.sberbank.ru",
        "tinkoff.ru", "alfabank.ru",
        "wildberries.ru", "ozon.ru", "avito.ru"
    );

    public BypassEngine(Action<string, LogLevel> log)
    {
        _log = log;
        _procManager = new ProcessManager(log);
    }

    public async Task<bool> StartAsync(List<string> presets)
    {
        try
        {
            _log("Настраиваю DNS...", LogLevel.Info);
            try { DnsManager.EnableSecureDns(_log); } catch { }

            _log("Извлекаю файлы...", LogLevel.Info);

            string? dir = null;
            try
            {
                dir = ExtractBinaries();
            }
            catch (Exception ex)
            {
                _log($"Ошибка извлечения: {ex.Message}", LogLevel.Error);
                return false;
            }

            if (dir == null)
            {
                _log("Не удалось извлечь файлы.", LogLevel.Error);
                return false;
            }

            _tempDir = dir;
            string winws = Path.Combine(dir, "obhod_core.exe");

            if (!File.Exists(winws))
            {
                _log("obhod_core.exe не найден.", LogLevel.Error);
                return false;
            }

            _log("Формирую конфигурацию...", LogLevel.Info);

            string args;
            try
            {
                args = BuildArgs(presets, dir);
            }
            catch (Exception ex)
            {
                _log($"Ошибка формирования аргументов: {ex.Message}", LogLevel.Error);
                return false;
            }

            if (string.IsNullOrEmpty(args))
            {
                _log("Пустая конфигурация.", LogLevel.Error);
                return false;
            }

            _log("Запускаю движок...", LogLevel.Info);

            bool ok;
            try
            {
                ok = await _procManager.StartProcessAsync(winws, args, "bypass");
            }
            catch (Exception ex)
            {
                _log($"Ошибка запуска движка: {ex.Message}", LogLevel.Error);
                return false;
            }

            if (ok)
                _log("Обход активен.", LogLevel.Success);
            else
                _log("Движок не запустился. Проверьте права администратора.", LogLevel.Error);

            return ok;
        }
        catch (Exception ex)
        {
            _log($"Непредвиденная ошибка: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    public async Task StopAsync()
    {
        try
        {
            _log("Останавливаю...", LogLevel.Info);
            await _procManager.StopAllAsync();
            try { DnsManager.DisableSecureDns(_log); } catch { }
            Cleanup();
            _log("Сеть восстановлена.", LogLevel.Success);
        }
        catch (Exception ex)
        {
            _log($"Ошибка: {ex.Message}", LogLevel.Warn);
        }
    }

    private string? ExtractBinaries()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"obhod_work_{Guid.NewGuid():N}");

        Directory.CreateDirectory(dir);

        var asm = Assembly.GetExecutingAssembly();

        var files = new[]
        {
            (RES_WINWS,    "obhod_core.exe"),
            (RES_CYGWIN,   "cygwin1.dll"),
            (RES_DIVERT,   "WinDivert.dll"),
            (RES_DIVERT64, "WinDivert64.sys"),
            (RES_BIN_QUIC, "quic.bin"),
            (RES_BIN_TLSS, "tls_google.bin"),
            (RES_BIN_TLS4, "tls_4pda.bin"),
        };

        foreach (var (res, name) in files)
        {
            string path = Path.Combine(dir, name);
            using var stream = asm.GetManifestResourceStream(res);
            if (stream == null)
            {
                _log($"Ресурс отсутствует: {name}", LogLevel.Warn);
                continue;
            }
            using var fs = File.Create(path);
            stream.CopyTo(fs);
        }

        return dir;
    }

    private static string BuildArgs(List<string> presets, string dir)
    {
        bool hasDiscord  = presets.Contains("discord");
        bool hasYoutube  = presets.Contains("youtube");
        bool hasRoblox   = presets.Contains("roblox");
        bool hasHttps    = presets.Contains("https");
        bool hasQuic     = presets.Contains("quic");
        bool hasUkraine  = presets.Contains("ukraine");

        string quic = $"\"{Path.Combine(dir, "quic.bin")}\"";
        string tlsG = $"\"{Path.Combine(dir, "tls_google.bin")}\"";
        string tls4 = $"\"{Path.Combine(dir, "tls_4pda.bin")}\"";

        var wfPortsTcp = new HashSet<string>();
        var wfPortsUdp = new HashSet<string>();

        if (hasDiscord)
        {
            wfPortsTcp.UnionWith(new[] { "80", "443", "2053", "2083", "2087", "2096", "8443" });
            wfPortsUdp.UnionWith(new[] { "443", "19294-19344", "50000-50100" });
        }
        if (hasYoutube || hasHttps)
        {
            wfPortsTcp.UnionWith(new[] { "80", "443", "8443" });
            wfPortsUdp.UnionWith(new[] { "443" });
        }
        if (hasRoblox)
        {
            wfPortsTcp.UnionWith(new[] { "80", "443", "1024-65535" });
            wfPortsUdp.UnionWith(new[] { "443", "1024-65535" });
        }
        if (hasQuic)
        {
            wfPortsUdp.UnionWith(new[] { "443" });
        }
        if (hasUkraine)
        {
            wfPortsTcp.UnionWith(new[] { "80", "443", "8443" });
            wfPortsUdp.UnionWith(new[] { "443" });
        }

        if (wfPortsTcp.Count == 0 && wfPortsUdp.Count == 0) return "";

        string tcpArgs = wfPortsTcp.Count > 0 ? $"--wf-tcp={string.Join(",", wfPortsTcp)}" : "";
        string udpArgs = wfPortsUdp.Count > 0 ? $"--wf-udp={string.Join(",", wfPortsUdp)}" : "";
        string wf = $"--wf-l3=ipv4,ipv6 {tcpArgs} {udpArgs}".Trim();

        var strategies = new List<string>();

        if (hasDiscord)
        {
            strategies.Add(
                "--filter-udp=443 " +
                $"--hostlist-domains={DiscordDomains} " +
                "--dpi-desync=fake --dpi-desync-repeats=6 " +
                $"--dpi-desync-fake-quic={quic}"
            );

            strategies.Add(
                "--filter-udp=19294-19344,50000-50100 " +
                "--filter-l7=discord,stun " +
                "--dpi-desync=fake --dpi-desync-repeats=6"
            );

            strategies.Add(
                "--filter-tcp=2053,2083,2087,2096,8443 " +
                "--hostlist-domains=discord.media " +
                "--dpi-desync=multisplit --dpi-desync-split-seqovl=681 " +
                "--dpi-desync-split-pos=1 " +
                $"--dpi-desync-split-seqovl-pattern={tlsG}"
            );

            strategies.Add(
                "--filter-tcp=80,443 " +
                $"--hostlist-domains={DiscordDomains} " +
                "--dpi-desync=multisplit --dpi-desync-split-seqovl=568 " +
                "--dpi-desync-split-pos=1 " +
                $"--dpi-desync-split-seqovl-pattern={tls4}"
            );
        }

        if (hasYoutube)
        {
            strategies.Add(
                "--filter-tcp=443 " +
                $"--hostlist-domains={YouTubeDomains} --ip-id=zero " +
                "--dpi-desync=multisplit --dpi-desync-split-seqovl=681 " +
                "--dpi-desync-split-pos=1 " +
                $"--dpi-desync-split-seqovl-pattern={tlsG}"
            );

            strategies.Add(
                "--filter-udp=443 " +
                $"--hostlist-domains={YouTubeDomains} " +
                "--dpi-desync=fake --dpi-desync-repeats=6 " +
                $"--dpi-desync-fake-quic={quic}"
            );
        }

        if (hasHttps)
        {
            strategies.Add(
                "--filter-udp=443 " +
                $"--hostlist-domains={GeneralDomains} " +
                "--dpi-desync=fake --dpi-desync-repeats=6 " +
                $"--dpi-desync-fake-quic={quic}"
            );

            strategies.Add(
                "--filter-tcp=80,443,8443 " +
                $"--hostlist-domains={GeneralDomains} " +
                "--dpi-desync=multisplit --dpi-desync-split-seqovl=568 " +
                "--dpi-desync-split-pos=1 " +
                $"--dpi-desync-split-seqovl-pattern={tls4}"
            );
        }

        if (hasRoblox)
        {
            strategies.Add(
                "--filter-tcp=443 " +
                $"--hostlist-domains={RobloxDomains} " +
                "--dpi-desync=multisplit --dpi-desync-split-seqovl=681 " +
                "--dpi-desync-split-pos=1 " +
                $"--dpi-desync-split-seqovl-pattern={tlsG}"
            );

            strategies.Add(
                "--filter-tcp=80,443 " +
                $"--hostlist-domains={RobloxDomains} " +
                "--dpi-desync=multisplit --dpi-desync-split-seqovl=568 " +
                "--dpi-desync-split-pos=1 " +
                $"--dpi-desync-split-seqovl-pattern={tls4}"
            );

            strategies.Add(
                "--filter-tcp=1024-65535 " +
                $"--hostlist-domains={RobloxDomains} " +
                "--dpi-desync=multisplit --dpi-desync-any-protocol=1 --dpi-desync-cutoff=n3 " +
                "--dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 " +
                $"--dpi-desync-split-seqovl-pattern={tls4}"
            );

            strategies.Add(
                "--filter-udp=443 " +
                $"--hostlist-domains={RobloxDomains} " +
                "--dpi-desync=fake --dpi-desync-repeats=6 " +
                $"--dpi-desync-fake-quic={quic}"
            );

            strategies.Add(
                "--filter-udp=1024-65535 " +
                $"--hostlist-domains={RobloxDomains} " +
                "--dpi-desync=fake --dpi-desync-repeats=12 --dpi-desync-any-protocol=1 " +
                $"--dpi-desync-fake-unknown-udp={quic} --dpi-desync-cutoff=n2"
            );
        }

        if (hasQuic)
        {
            strategies.Add(
                "--filter-udp=443 " +
                $"--hostlist-domains={GeneralDomains} " +
                "--dpi-desync=fake --dpi-desync-repeats=6 " +
                $"--dpi-desync-fake-quic={quic}"
            );
        }

        if (hasUkraine)
        {
            strategies.Add(
                "--filter-tcp=443 " +
                $"--hostlist-domains={UkraineDomains} " +
                "--dpi-desync=multisplit --dpi-desync-split-seqovl=681 " +
                "--dpi-desync-split-pos=1 " +
                $"--dpi-desync-split-seqovl-pattern={tlsG}"
            );

            strategies.Add(
                "--filter-tcp=80,443,8443 " +
                $"--hostlist-domains={UkraineDomains} " +
                "--dpi-desync=multisplit --dpi-desync-split-seqovl=568 " +
                "--dpi-desync-split-pos=1 " +
                $"--dpi-desync-split-seqovl-pattern={tls4}"
            );

            strategies.Add(
                "--filter-udp=443 " +
                $"--hostlist-domains={UkraineDomains} " +
                "--dpi-desync=fake --dpi-desync-repeats=6 " +
                $"--dpi-desync-fake-quic={quic}"
            );
        }

        if (strategies.Count == 0) return "";

        return wf + " " + string.Join(" --new ", strategies);
    }

    private void Cleanup()
    {
        if (_tempDir == null || !Directory.Exists(_tempDir)) return;
        try { Directory.Delete(_tempDir, true); _tempDir = null; } catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _procManager.StopAll(); } catch { }
        Cleanup();
    }
}
