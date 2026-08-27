using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace Veil.Services;

public enum TunnelState { Disconnected, Connecting, Connected, Disconnecting, Error }

public class WireGuardNotFoundException : Exception
{
    public WireGuardNotFoundException()
        : base("WireGuard for Windows isn't installed. Get it from wireguard.com/install and try again.") { }
}

public class WireGuardService
{
    private static readonly string[] KnownPaths =
    {
        @"C:\Program Files\WireGuard\wireguard.exe",
        @"C:\Program Files (x86)\WireGuard\wireguard.exe",
    };

    private string? _exePath;

    public string ExePath => _exePath ??= FindWireGuard();

    private static string FindWireGuard()
    {
        foreach (var path in KnownPaths)
            if (File.Exists(path))
                return path;

        throw new WireGuardNotFoundException();
    }

    public static string ServiceNameFor(string tunnelName) => $"WireGuardTunnel${tunnelName}";

    private static ServiceController? TryGetService(string tunnelName)
    {
        var name = ServiceNameFor(tunnelName);
        try
        {
            var sc = new ServiceController(name);
            _ = sc.Status; // throws InvalidOperationException if it doesn't exist
            return sc;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public TunnelState GetState(string tunnelName)
    {
        var sc = TryGetService(tunnelName);
        if (sc == null) return TunnelState.Disconnected;

        return sc.Status switch
        {
            ServiceControllerStatus.Running => TunnelState.Connected,
            ServiceControllerStatus.StartPending => TunnelState.Connecting,
            _ => TunnelState.Disconnected,
        };
    }

    public async Task ConnectAsync(string tunnelName, string configPath)
    {
        var existing = TryGetService(tunnelName);
        if (existing == null)
        {
            // first connect: this installs a windows service for the tunnel
            // and starts it in one step
            await RunElevatedInline(ExePath, $"/installtunnelservice \"{configPath}\"");
            return;
        }

        if (existing.Status != ServiceControllerStatus.Running)
        {
            existing.Start();
            await Task.Run(() => existing.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15)));
        }
    }

    public async Task DisconnectAsync(string tunnelName)
    {
        var sc = TryGetService(tunnelName);
        if (sc == null) return;
        if (sc.Status == ServiceControllerStatus.Stopped) return;

        sc.Stop();
        await Task.Run(() => sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15)));
    }

    public async Task RemoveAsync(string tunnelName)
    {
        var sc = TryGetService(tunnelName);
        if (sc != null && sc.Status != ServiceControllerStatus.Stopped)
        {
            sc.Stop();
            await Task.Run(() => sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15)));
        }

        await RunElevatedInline(ExePath, $"/uninstalltunnelservice {tunnelName}");
    }

    // the app itself already runs elevated (see app.manifest), so this is
    // just a normal process start, no separate UAC prompt needed
    private static Task RunElevatedInline(string exe, string args)
    {
        return Task.Run(() =>
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            using var proc = Process.Start(psi)!;
            proc.WaitForExit(20000);
            if (proc.ExitCode != 0)
            {
                var err = proc.StandardError.ReadToEnd();
                throw new InvalidOperationException($"wireguard.exe exited with {proc.ExitCode}: {err}");
            }
        });
    }
}
