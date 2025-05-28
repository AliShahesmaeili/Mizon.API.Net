namespace Mizon.API.Client.Responses;

public class MeResponse : IApiResponse
{
    public int UserId { get; set; }
    public string? ProfileImageURL { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string UserName { get; set; } = null!;
}
