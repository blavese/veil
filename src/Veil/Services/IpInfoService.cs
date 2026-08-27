using System.Net.Http;

namespace Veil.Services;

public class IpInfoService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

    public async Task<string> GetPublicIpAsync()
    {
        try
        {
            return await _http.GetStringAsync("https://api.ipify.org");
        }
        catch
        {
            return "unknown";
        }
    }
}
