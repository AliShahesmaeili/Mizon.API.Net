namespace Mizon.API;

/// <summary>
/// Represents the different compression methods that can be used.
/// </summary>
public enum CompressionMethod
{
    /// <summary>
    /// No compression is applied.
    /// </summary>
    None,

    /// <summary>
    /// Uses the GZip compression algorithm.
    /// </summary>
    GZip,

    /// <summary>
    /// Uses the Deflate compression algorithm.
    /// </summary>
    Deflate
}
