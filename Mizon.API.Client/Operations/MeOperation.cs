namespace Mizon.API.Client.Operations;

public class MeOperation : MizonApiRequest<MeRequest, MeResponse>
{
    public MeOperation(MeRequest request) : base(HttpMethod.GET, Endpoints.ME, request)
    {
        MaximumClientCacheDuration = TimeSpan.FromSeconds(10);
    }
}
