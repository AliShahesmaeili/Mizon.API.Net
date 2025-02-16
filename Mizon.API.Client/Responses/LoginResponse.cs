namespace Mizon.API.Client.Responses;

/// <summary>
/// Represents response from login operation.
/// </summary>
public class LoginResponse : IApiResponse
{
    /// <summary>
    /// The authentication token.
    /// </summary>
    public string Token { get; set; } = null!;
}