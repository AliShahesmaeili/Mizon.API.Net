namespace Mizon.API.Client.Methods;

public class LoginMethod : MizonApiRequest<LoginRequest, LoginResponse>
{
    public LoginMethod(LoginRequest loginRequest) : base(HttpMethod.POST, Endpoints.Login, loginRequest)
    {
        NeedAuthorized = false;
        MaximumClientCacheDuration = TimeSpan.FromSeconds(122);
        PropertyForToken = (response) => response.Token;
    }
}
