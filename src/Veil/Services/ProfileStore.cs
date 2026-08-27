using System.IO;
using System.Text.Json;
using Veil.Models;

namespace Veil.Services;

// keeps imported configs and a small metadata file under %LocalAppData%\Veil
public class ProfileStore
{
    private readonly string _root;
    private readonly string _configsDir;
    private readonly string _metaPath;

    public ProfileStore()
    {
        _root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Veil");
        _configsDir = Path.Combine(_root, "tunnels");
        _metaPath = Path.Combine(_root, "profiles.json");
        Directory.CreateDirectory(_configsDir);
    }

    public List<TunnelProfile> Load()
    {
        if (!File.Exists(_metaPath)) return new List<TunnelProfile>();
        var json = File.ReadAllText(_metaPath);
        return JsonSerializer.Deserialize<List<TunnelProfile>>(json) ?? new List<TunnelProfile>();
    }

    public void Save(List<TunnelProfile> profiles)
    {
        var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_metaPath, json);
    }

    // copies the source .conf into our own storage so the original file the
    // user picked can move or get deleted without breaking the tunnel
    public TunnelProfile Import(string sourcePath)
    {
        var name = Path.GetFileNameWithoutExtension(sourcePath);
        var destPath = Path.Combine(_configsDir, $"{name}.conf");

        var suffix = 2;
        while (File.Exists(destPath))
        {
            destPath = Path.Combine(_configsDir, $"{name}-{suffix}.conf");
            suffix++;
        }

        File.Copy(sourcePath, destPath);
        return WireGuardConfigParser.Parse(destPath);
    }

    public void DeleteConfig(TunnelProfile profile)
    {
        if (File.Exists(profile.ConfigPath))
            File.Delete(profile.ConfigPath);
    }
}
