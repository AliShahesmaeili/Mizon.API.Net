using Mizon.API;

namespace ConsoleApp1;

public class LoginMethod : MizonApiRequest<LoginRequest, LoginResponse>
{
    public LoginMethod(LoginRequest loginRequest) : base(Mizon.API.HttpMethod.POST, "https://localhost:7140/WeatherForecast/MyAction", loginRequest)
    {
        NeedAuthorized = false;
        MaximumClientCacheDuration = TimeSpan.FromSeconds(122);
    }
}
