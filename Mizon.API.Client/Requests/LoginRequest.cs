namespace Mizon.API.Client.Requests;

/// <summary>
/// Represents request to login operation.
/// </summary>
public class LoginRequest : IApiRequest
{
    /// <summary>
    /// Username for the login operation.
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// password for the login operation.
    /// </summary>
    public string Password { get; set; } = null!;
}