namespace Mizon.API;

/// <summary>
/// Defines the different caching strategies for client requests.
/// </summary>
public enum ClientCacheStrategy
{
    /// <summary>
    /// Caching is completely disabled. Each request will always be sent to the server.
    /// </summary>
    Disabled,

    /// <summary>
    /// Caching is enabled. Responses will be cached and served from the cache if available.
    /// </summary>
    Enabled,

    /// <summary>
    /// Caching is enabled, but only for duplicate requests.
    /// The first request will be sent to the server, and subsequent identical requests
    /// will be served from the cache until the cache entry expires or is invalidated.
    /// </summary>
    EnabledOnDuplicateRequest
}
