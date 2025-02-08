using Mizon.API;

namespace ConsoleApp1;

public class LoginRequest : IApiRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}
