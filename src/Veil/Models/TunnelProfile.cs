namespace Veil.Models;

public class TunnelProfile
{
    public string Name { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string Address { get; set; } = "";
    public string Dns { get; set; } = "";
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    // full path to the .conf file this profile was built from
    public string ConfigPath { get; set; } = "";
}
