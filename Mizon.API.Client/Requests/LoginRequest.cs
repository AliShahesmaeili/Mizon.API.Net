namespace Mizon.API.Client.Requests;

public class LoginRequest : IApiRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}
