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

    private const string HEX_QUIC       = "0xc3000000010878e79846bbf379840000449eb2e21b4d8bcc2fc93eef8149954a5ba6364b91191b7d0ee93f38ff9192fd16efb7a203e543c77231e3fcf68f00bb2fc34b065aa245df19eb8ec284a77d1b507b73b37192bc8a6a3d9858260c1c6bfb2d7429bc3ca1d6d01c79372932910c3d3ff20e5224091004829634baab8d9dfed6d0aaf69e8670301cdeaa3e502c272640fa4bedf32432a9fd3eb4e4e04d5d9d02593efc6867faa7b4b8a987d9221953c1a2e90b70d17e7a14e9f8ce79a9429bcd93a2e235e4e673d9275d78033de032e9773a51e91b22179d32be9e978461abb46e1aa9557e4b35ebc3152483232b2fc863325106bd18d74bb28755d6b558710435969b01a9699c46d2638fbbabe4ebdb591619d9e96976d250773d6fe9bcf848d9f6e49e16201a4d9baa8c2ae0f00c2afdb33d926c431db8f44ab5f1192528bca7019a3420ecdf0deb8d52b5b7741580b630a10b9d8b51dd96832c2c33d1e66c24acdefea87eb5af114a6d04f7b18f70a5c4d7b65d6df56dfde17eefb02af89f399d6c650a178b7eb049a8d3760c36d5e7ab8aabff552161d1dbd4d763fbd6434507ccf9f92a91bd54ed8ce308cccd9a0d7e04d8f58700f82955b8538cd011ee0a0eb1d8b02db11f3e39fafa174861fc12734e62f5f9d5a8ce7aa109e5be3c2e31f4d41d7bcb528536e7845a70b8244bd52d1cf8fcb6aef409b46cdb8fd3500b3fa31da493a59df985b75069b8dbea35ad281d96b3fb5cdd64998335554972c3d9d3a0c170b06e1a7bbdb12c43304f006f33720d359a9df35776c0f6ed0deba35ed289f9637d1423bf5c5ec061c2eaa2be6616db1a47353fa8f0b2fb6619a29b24881317b22c359239249f3eedf7b5f7e7225e0f8e5e3ca741b705176bdd29ea8665b50d014a9755ce0d11104c0c0be5d62ed3436067d804f4d1e3e941afd168a6eeaf455732d32b70b49d4aecd8454f05984e70a68e80e1f3f0be13d3abcad3685a13e27f9f536c2b141f7c6f5e3ac66f998fc6f2200f778ae745057cc35bc43e0730c8882901f4443031230d9a3444ba40756e074348019ae16378a490df24508516b0bb89a4ad939feaf7d9452e96b7be30c74b38094494c15cb253e5a01cc3e7593ae58e5928cfad4b1d70ed79d1196e0fe50f5be3037a6ddbfad8998c66cfbf8ff3ed28cd8bb8580951c46f81c0c5993f4aaf9f035036d6141076fd8bf7df295716fc81ceeff9d249be09208ff3e7ea07ee8b485551d212a871740599b03cca8c21489e9463c462f76b02411e7abe50bc44077fa741b2fab100a37b1046a1c212009f6849722d51c3dcead071780fcbf7ba0c3a93b214c8d1fce4d83564498ca703130ed4ba044e39123e38302c3a980bb5ee01cb622c1b2329ec486c8b8e59358813f8e95e02049d90ad0421b916810137d0d0152044686ec8fa146b8828bce5a551086f6f7a1413bb3db9c95f7c2d3239f7cc5d704f194a5b224973f42c84a20576ef6238ace6c74f53f9126e340a70b6cd484c6b510dcfd18875fa66e8a1c70da564788a5172b2b8dcf51333a3f2c85d9c3f029046b094ffb7d35c4a03d1a16beaa9ed2783742f8aa851ca0c5c649273aa381dd90c95228cbfbd9fb45b0eedf64c69c469a242904f95404afde30a0d03c4b18a016f5bb74ab691064aa3ed89cc95050b222346ad0c7056";
    private const string HEX_TLS_GOOGLE = "0x16030102a4010002a00303531b5b9735d271efc6c08b3cc02b031b8aafa9927d2eeba875776f66f6cacc5420adc823baefc13221e7a7326e2e595c7701b059eb689681299e1584eae0aa76ca0022130113031302c02bc02fcca9cca8c02cc030c00ac009c013c014009c009d002f00350100023500000013001100000e7777772e676f6f676c652e636f6d00170000ff01000100000a000e000c001d00170018001901000101000b00020100002300000010000e000c02683208687474702f312e310005000501000000000022000a00080403050306030203001200000033006b0069001d002073d4ab37346437255e7e2066411d8fd7ccc66443219ecdfd868901285cc45d7f0017004104da2b8432221e30db3b1c62bda63323bc455a7e7bee17f57534b738385bfe4dd4c4407c968da02d9d3ac0fe4a9002fd57b5fa53ce976a89f9bf4642927e1f0185002b0009080304030303020301000d0018001604030503060308040805080604010501060102030201002d00020101001c00024001001b000706000100020003fe0d01190000010003c90020d6d1f94095d0b691ac9cb678b25dd6cd78ca9dc491948d0fc1032fb2b51dbb3d00efe8cdfea6dbe90e47ba2690aaf493fe8cba74bc2eba6ed661e8478b5132ae901d4d0b7facc3b84dd1381bfebd2ccb685ae546bf0c7792d2bd77b732b1e9ac50b4a75e7f76e544cdc44c5e3315d162661fb132b2480c0bf4485f34d23e072e815823ea926c2e2d231c6ef3e07a8f25da009c46f006ec8c56164090f3b6ba6df957c76eb1fc4a5f436ecb3faf6465e771fc19939f862307eb98c13afa1f5d394a8a7c3313294cccedc1b872de44e2d5620488349cadd325f68bd3b2d9938fd1dbdc4a54064daaae055bd19ffcbef9adf93036035fdd082209147ee018a7cdb626fb4ba2dcebeb5ed8c283a0ca891a6b7b";
    private const string HEX_TLS_4PDA   = "0x1603010117010001130303d5ad552b42c4c6a2d54622f31b3c41f6e5d9f48f866d36c46e7da92d4010dc7020f9aedd547996a5b68d8ddeba00abc0735dd66440c1b26aba66dba0fecc7f1c33002813021301c02cc02bc030c02fc024c023c028c027c00ac009c014c013009d009c003d003c0035002f010000a20000000c000a000007347064612e746f000500050100000000002b00050403040303000d001a001808040805080604010501020104030503020302020601060300230000000a00080006001d00170018000b000201000010000b000908687474702f312e31003300260024001d0020da0fc18ad8f155a28f26d1be4f3261dd3d593da905e9afdbd9146df55879d65e0031000000170000ff01000100002d00020101";

    private static readonly string DiscordDomains = string.Join(",",
        "discord.com", "discordapp.com", "discord.gg", "discordapp.net",
        "cdn.discordapp.com", "media.discordapp.net", "images.discordapp.net",
        "gateway.discord.gg", "status.discord.com", "discordcdn.com", "discord.media"
    );

    private static readonly string YouTubeDomains = string.Join(",",
        "youtube.com", "youtu.be", "googlevideo.com", "ytimg.com",
        "i.ytimg.com", "yt3.ggpht.com", "youtubei.googleapis.com",
        "googleapis.com", "gvt1.com"
    );

    private static readonly string RobloxDomains = string.Join(",",
        "roblox.com", "rbxcdn.com", "images.rbxcdn.com", "static.rbxcdn.com",
        "assetdelivery.roblox.com", "roblox-player.com", "clientsettings.roblox.com",
        "setup.roblox.com", "rbxtrk.com", "rbxstatic.com", "robloxlabs.com", "rbx.com"
    );

    private static readonly string GeneralDomains = string.Join(",",
        "cloudflare.com", "cdnjs.cloudflare.com", "akamaihd.net", "akamaized.net", "fastly.net"
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
        bool hasDiscord = presets.Contains("discord");
        bool hasYoutube = presets.Contains("youtube");
        bool hasRoblox  = presets.Contains("roblox");
        bool hasHttps   = presets.Contains("https");
        bool hasQuic    = presets.Contains("quic");

        string quic   = $"\"{Path.Combine(dir, "quic.bin")}\"";
        string tlsG   = $"\"{Path.Combine(dir, "tls_google.bin")}\"";
        string tls4   = $"\"{Path.Combine(dir, "tls_4pda.bin")}\"";

        var wfPortsTcp = new HashSet<string>();
        var wfPortsUdp = new HashSet<string>();

        if (hasDiscord)
        {
            wfPortsTcp.UnionWith(new[] { "80", "443", "2053", "2083", "2087", "2096", "8443" });
            wfPortsUdp.UnionWith(new[] { "443", "19294-19344", "50000-65535" });
        }
        if (hasYoutube || hasHttps)
        {
            wfPortsTcp.UnionWith(new[] { "80", "443" });
            wfPortsUdp.UnionWith(new[] { "443" });
        }
        if (hasRoblox)
        {
            wfPortsTcp.UnionWith(new[] { "1024-65535" });
            wfPortsUdp.UnionWith(new[] { "1024-65535" });
        }
        if (hasQuic)
        {
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
                "--filter-udp=19294-19344,50000-65535 " +
                "--filter-l7=discord,stun " +
                "--dpi-desync=fake --dpi-desync-repeats=6"
            );

            strategies.Add(
                "--filter-tcp=2053,2083,2087,2096,8443 " +
                $"--hostlist-domains={DiscordDomains} " +
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

            strategies.Add(
                "--filter-udp=443 " +
                $"--hostlist-domains={DiscordDomains} " +
                "--dpi-desync=fake --dpi-desync-repeats=6 " +
                $"--dpi-desync-fake-quic={quic}"
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
                "--filter-tcp=1024-65535 " +
                $"--hostlist-domains={RobloxDomains} " +
                "--dpi-desync=multisplit --dpi-desync-any-protocol=1 --dpi-desync-cutoff=n3 " +
                "--dpi-desync-split-seqovl=568 --dpi-desync-split-pos=1 " +
                $"--dpi-desync-split-seqovl-pattern={tls4}"
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
