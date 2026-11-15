using System.Net;

namespace EquipmentLibraryV2_Avalonia.Services;

public abstract class CreateHttpClient
{
    public static HttpClient Shared { get; } = Create();
    
    private static HttpClient Create()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(1),
            ConnectTimeout = TimeSpan.FromSeconds(10)
        };

        return new HttpClient(handler)
        {
            DefaultRequestVersion = HttpVersion.Version30,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
}