using System.IO;
using Veil.Models;

namespace Veil.Services;

public class ConfigParseException : Exception
{
    public ConfigParseException(string message) : base(message) { }
}

public static class WireGuardConfigParser
{
    // pulls out the bits we need to show in the UI. doesn't validate every
    // field, just enough to catch someone importing a random text file.
    public static TunnelProfile Parse(string path)
    {
        var text = File.ReadAllText(path);
        var lines = text.Split('\n').Select(l => l.Trim()).ToList();

        string section = "";
        var values = new Dictionary<string, string>();
        bool sawInterface = false, sawPeer = false;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                section = line[1..^1].Trim().ToLowerInvariant();
                if (section == "interface") sawInterface = true;
                if (section == "peer") sawPeer = true;
                continue;
            }

            var idx = line.IndexOf('=');
            if (idx < 0) continue;

            var key = $"{section}.{line[..idx].Trim().ToLowerInvariant()}";
            var value = line[(idx + 1)..].Trim();
            values[key] = value;
        }

        if (!sawInterface || !sawPeer)
            throw new ConfigParseException("Doesn't look like a WireGuard config (missing [Interface] or [Peer]).");

        if (!values.ContainsKey("interface.privatekey"))
            throw new ConfigParseException("Missing PrivateKey under [Interface].");

        if (!values.ContainsKey("peer.publickey"))
            throw new ConfigParseException("Missing PublicKey under [Peer].");

        var name = Path.GetFileNameWithoutExtension(path);

        return new TunnelProfile
        {
            Name = name,
            Address = values.GetValueOrDefault("interface.address", ""),
            Dns = values.GetValueOrDefault("interface.dns", ""),
            Endpoint = values.GetValueOrDefault("peer.endpoint", ""),
            ConfigPath = path,
        };
    }
}
