namespace Mizon.API;

/// <summary>
/// Represents a base class for API requests, providing a container for the request content.
/// </summary>
/// <typeparam name="TApiRequest">The specific type of API request, which must implement the <see cref="IApiRequest"/> interface.</typeparam>
public class BaseApiRequest<TApiRequest> where TApiRequest : IApiRequest
{
    /// <summary>
    /// The content of the API request.
    /// This property holds the specific data for the API request.
    /// </summary>
    public TApiRequest? RequestContent { get; set; }
}
