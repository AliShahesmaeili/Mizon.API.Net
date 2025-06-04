namespace Mizon.API.Client.Operations;

public class MusicItemOperation : MizonApiRequest<MusicItemRequest, MusicItemResponse>
{
    public MusicItemOperation(MusicItemRequest request) : base(HttpMethod.GET, Endpoints.ME, request)
    {
        MaximumClientCacheDuration = TimeSpan.FromSeconds(10);
    }
}
