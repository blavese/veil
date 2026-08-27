using System.Diagnostics;

namespace Veil.Services;

// blocks all outbound traffic except through the tunnel adapter, so if the
// tunnel drops nothing leaks out over the regular connection.
public class FirewallService
{
    private const string RuleName = "Veil-KillSwitch-Allow";

    public bool IsEnabled { get; private set; }

    public async Task EnableAsync(string tunnelAdapterName)
    {
        await RunPowerShell(
            $"New-NetFirewallRule -DisplayName '{RuleName}' -Direction Outbound " +
            $"-InterfaceAlias '{tunnelAdapterName}' -Action Allow -Profile Any " +
            "-ErrorAction SilentlyContinue | Out-Null"
        );

        await RunNetsh("advfirewall set allprofiles firewallpolicy blockoutbound,allowinbound");
        IsEnabled = true;
    }

    public async Task DisableAsync()
    {
        await RunPowerShell(
            $"Remove-NetFirewallRule -DisplayName '{RuleName}' -ErrorAction SilentlyContinue | Out-Null"
        );

        await RunNetsh("advfirewall set allprofiles firewallpolicy blockinbound,allowoutbound");
        IsEnabled = false;
    }

    private static Task RunNetsh(string args) => RunProcess("netsh.exe", args);

    private static Task RunPowerShell(string command) =>
        RunProcess("powershell.exe", $"-NoProfile -NonInteractive -Command \"{command}\"");

    private static Task RunProcess(string exe, string args)
    {
        return Task.Run(() =>
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            };
            using var proc = Process.Start(psi)!;
            proc.WaitForExit(15000);
        });
    }
}
