using Mizon.API.Attributes;

namespace Mizon.API.Client.Responses;

[RealtimeEntity("Me", "SubscribeToMe", "UnsubscribeFromMe")]
public class MeResponse : IApiResponse
{
    [RealtimeEntityId]
    public int UserId { get; set; }

    [RealtimeUpdatable]
    public string? ProfileImageURL { get; set; }

    [RealtimeUpdatable]
    public string? FirstName { get; set; }

    [RealtimeUpdatable]
    public string? LastName { get; set; }

    [RealtimeUpdatable]
    public string UserName { get; set; } = null!;
}
