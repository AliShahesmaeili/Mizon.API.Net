using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Mizon.API.Client;

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

        var hubConnection = new HubConnectionBuilder()
            .WithUrl(Endpoints.BASE_HUB_URL)
            .WithAutomaticReconnect()
            .Build();

        services.AddSingleton(hubConnection);

        services.AddSingleton<MizonApi>();
        return services;
    }
}
