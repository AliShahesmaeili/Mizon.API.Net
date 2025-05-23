using Microsoft.Extensions.Caching.Memory;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Mizon.API;

public class MizonApi
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IMemoryCache _memoryCache;
    private string _token;

    public MizonApi(HttpClient httpClient, IMemoryCache memoryCache)
    {
        _httpClient = httpClient;
        _memoryCache = memoryCache;
    }

    public async Task<BaseApiResponse<Response>> SendRequestAsync<Request, Response>(MizonApiRequest<Request, Response> mizonApiRequest, CancellationToken? cancellationToken = null) where Request : IApiRequest where Response : IApiResponse
    {
        var baseApiResponse = new BaseApiResponse<Response>();

        try
        {
            var cacheKey = ManageCacheKey(mizonApiRequest.BaseApiRequest, mizonApiRequest.Endpoint, mizonApiRequest.HttpMethod);

            var cachedData = ManageGetCache(cacheKey, mizonApiRequest.ClientCacheStrategy);

            if (cachedData is BaseApiResponse<Response> cachedResponse)
                return cachedResponse;

            var httpRequestMessage = new HttpRequestMessage();

            ManageHttpVersion(httpRequestMessage);

            ManageAuthorization(httpRequestMessage, mizonApiRequest.NeedAuthorized, _token);



            if (mizonApiRequest.HttpMethod == HttpMethod.GET)
            {
                //var queryString = GetQueryStringFromObject(mizonApiRequest.BaseApiRequest);
                //httpRequestMessage.RequestUri = new($"{mizonApiRequest.Endpoint}?{queryString}");
                //httpRequestMessage.Method = System.Net.Http.HttpMethod.Get;
            }
            else if (mizonApiRequest.HttpMethod == HttpMethod.POST)
            {
                httpRequestMessage.RequestUri = new(mizonApiRequest.Endpoint);
                httpRequestMessage.Method = System.Net.Http.HttpMethod.Post;

                string requestJson = JsonSerializer.Serialize(mizonApiRequest.BaseApiRequest, JsonOptions);
                ManageCompression(httpRequestMessage, requestJson, mizonApiRequest.CompressionMethod);
            }
            else throw new NotImplementedException();



            using var timeoutCts = new CancellationTokenSource(mizonApiRequest.CallTimeoutDuration);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken ?? CancellationToken.None);


            var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage, linkedCts.Token);



            // httpResponseMessage.EnsureSuccessStatusCode();

            var responseJson = await httpResponseMessage.Content.ReadAsStringAsync();


            var response = JsonSerializer.Deserialize<BaseApiResponse<Response>>(responseJson, JsonOptions)!;

            ManageToken(mizonApiRequest.PropertyForToken, response);

            ManageSetCache(cacheKey, response, mizonApiRequest.ClientCacheStrategy, mizonApiRequest.MaximumClientCacheDuration);

            return response;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            baseApiResponse.Error = new() { Code = 1, Message = "Operation cancelled by user: " + ex.Message };
        }
        catch (TaskCanceledException ex)
        {
            baseApiResponse.Error = new() { Code = 2, Message = "Request timed out: " + ex.Message };
        }
        catch (HttpRequestException ex)
        {
            baseApiResponse.Error = new() { Code = 3, Message = "HTTP error: " + ex.Message };
        }
        catch (Exception ex)
        {
            baseApiResponse.Error = new() { Code = 4, Message = "Unexpected error: " + ex.Message };
        }

        return baseApiResponse;
    }

    // تابع برای تولید کلید منحصر به فرد برای کش
    private string ManageCacheKey(object data, string endpoint, HttpMethod httpMethod)
    {
        var requestHash = JsonSerializer.Serialize(data).GetHashCode();
        return $"{endpoint}_{httpMethod}_{requestHash}";
    }

    // تابع برای بررسی درخواست تکراری
    private bool IsDuplicateRequest(string cacheKey)
    {
        // بررسی وجود کلید درخواست در کش
        return _memoryCache.TryGetValue(cacheKey, out _);
    }


    private void ManageAuthorization(HttpRequestMessage httpRequestMessage, bool needAuthorized, string? token = null)
    {
        if (needAuthorized)
        {
            if (!string.IsNullOrEmpty(token))
                httpRequestMessage.Headers.Add("Authorization", token);
            else throw new Exception("This method need authorized!");
        }
    }

    private void ManageToken<TApiResponse>(Func<TApiResponse, string>? propertyForToken, BaseApiResponse<TApiResponse>? baseApiResponse) where TApiResponse : IApiResponse
    {
        if (propertyForToken is null)
            return;

        if (baseApiResponse is null)
            return;

        _token = propertyForToken(baseApiResponse.ResponseContent!);
    }

    private void ManageHttpVersion(HttpRequestMessage httpRequestMessage)
    {
        httpRequestMessage.Version = HttpVersion.Version30;
    }

    private void ManageCompression(HttpRequestMessage httpRequestMessage, string value, CompressionMethod compressionMethod)
    {
        using var memoryStream = new MemoryStream();

        var jsonBytes = Encoding.UTF8.GetBytes(value);

        if (compressionMethod is CompressionMethod.None)
        {
            memoryStream.Write(jsonBytes);
        }
        else if (compressionMethod is CompressionMethod.GZip)
        {
            using var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress);
            gZipStream.Write(jsonBytes);
        }
        else if (compressionMethod is CompressionMethod.Deflate)
        {
            using var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress);
            deflateStream.Write(jsonBytes);
        }

        var byteArrayContent = new ByteArrayContent(memoryStream.ToArray());
        byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        if (compressionMethod is CompressionMethod.GZip)
            byteArrayContent.Headers.ContentEncoding.Add("gzip");

        else if (compressionMethod is CompressionMethod.Deflate)
            byteArrayContent.Headers.ContentEncoding.Add("deflate");

        else byteArrayContent.Headers.ContentEncoding.Clear();

        httpRequestMessage.Content = byteArrayContent;
    }

    private void ManageSetCache(string cacheKey, object? data, ClientCacheStrategy clientCacheStrategy, TimeSpan? maximumClientCacheDuration = null)
    {
        if (clientCacheStrategy is ClientCacheStrategy.Disabled)
            return;

        if (data is null)
            return;

        var memoryCacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = maximumClientCacheDuration ?? TimeSpan.FromMinutes(5)
        };

        _memoryCache.Set(cacheKey, data, memoryCacheEntryOptions);
    }

    private object? ManageGetCache(string cacheKey, ClientCacheStrategy clientCacheStrategy)
    {
        if (clientCacheStrategy is not ClientCacheStrategy.Disabled)
        {
            if (_memoryCache.TryGetValue(cacheKey, out var cachedResponse))
            {
                if (clientCacheStrategy is ClientCacheStrategy.Enabled ||
                    (clientCacheStrategy is ClientCacheStrategy.EnabledOnDuplicateRequest && IsDuplicateRequest(cacheKey)))
                {
                    return cachedResponse;
                }
            }
        }

        return null;
    }

}