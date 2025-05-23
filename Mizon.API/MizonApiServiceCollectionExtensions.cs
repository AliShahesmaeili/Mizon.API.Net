using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Mizon.API;

public static class MizonApiServiceCollectionExtensions
{
    public static IServiceCollection AddMizonApi(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient<MizonApi>(client => { })
             .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
             {
                 AutomaticDecompression = DecompressionMethods.All
             });
        services.AddSingleton<MizonApi>();
        return services;
    }
}
