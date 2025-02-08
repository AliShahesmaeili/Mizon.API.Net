namespace Mizon.API;

/// <summary>
/// Represents a base error response from the API.
/// </summary>
public class BaseApiError
{
    /// <summary>
    /// The error code returned by the API.
    /// </summary>
    public int Code { get; set; }


    /// <summary>
    /// A human-readable message describing the error.
    /// </summary>
    public string Message { get; set; } = null!;
}
