# veil

Minimal WireGuard client for Windows. Import a config, connect, done.

**[Download the latest release](https://github.com/blavese/veil/releases/latest)**, run `Veil.exe`, allow the admin prompt. No .NET install needed, the runtime is bundled in. [WireGuard for Windows](https://www.wireguard.com/install/) needs to be installed separately, Veil drives it rather than replacing it.

Not a VPN service. Doesn't ship servers or keys. Point it at any WireGuard config (your own server, a provider's config export, whatever) and it manages the tunnel for you.

## features

- Import `.conf` files, manage multiple tunnels
- One-click connect/disconnect
- Kill switch: blocks outbound traffic if the tunnel drops
- Shows public IP and connection time while active
- Dark, minimal UI. No telemetry, no accounts, no ads.

## how it works

Veil is a GUI wrapper around the official WireGuard for Windows tunnel service. It doesn't implement the WireGuard protocol itself. It shells out to `wireguard.exe` to install/start/stop tunnel services and uses Windows Firewall rules for the kill switch.

## requirements

- Windows 10/11, x64
- [WireGuard for Windows](https://www.wireguard.com/install/) installed
- .NET 8 runtime (or build from source with the SDK)

Veil needs to run as administrator since managing tunnel services and firewall rules both require it. The app manifest requests elevation automatically.

## building

```bash
git clone https://github.com/blavese/veil.git
cd veil/src/Veil
dotnet build
```

Run `Veil.exe` from `bin/Debug/net8.0-windows/`, or `dotnet run` from an elevated terminal.

## importing a config

Click "Import config" and pick a `.conf` file. Veil copies it into its own storage (`%LocalAppData%\Veil`) so the original file can move or get deleted without breaking the tunnel.

## kill switch

When enabled, sets the Windows Firewall default outbound policy to block and adds a single allow rule scoped to the tunnel adapter. Disabling it restores the default policy. If Veil crashes while the kill switch is on, re-run it and toggle the switch off to restore normal firewall behavior.

## license

MIT, see [LICENSE](LICENSE).
