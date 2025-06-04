namespace Mizon.API.Client.Responses;


public class MusicItemResponse : IApiResponse
{
    public string Title { get; set; } = null!;
    public string Url { get; set; } = null!;

    public string LikeCount { get; set; } = null!;
    public string PlayingCount { get; set; } = null!;

}
