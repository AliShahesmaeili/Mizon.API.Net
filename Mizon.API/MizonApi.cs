using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Mizon.API;

public class MizonApi
{
    private HttpClient _httpClient = new(new HttpClientHandler()
    {
        AutomaticDecompression = DecompressionMethods.All,
    });

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IMemoryCache _memoryCache;

    public MizonApi()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
    }

    public async Task<BaseApiResponse<Response>> SendRequestAsync<Request, Response>(MizonApiRequest<Request, Response> mizonApiRequest, CancellationToken? cancellationToken = null) where Request : IApiRequest where Response : IApiResponse
    {
        var baseApiResponse = new BaseApiResponse<Response>();

        try
        {
            // تولید کلید منحصر به فرد برای کش بر اساس درخواست
            var cacheKey = GenerateCacheKey(mizonApiRequest);

            // بررسی استراتژی کش
            if (mizonApiRequest.ClientCacheStrategy != ClientCacheStrategy.Disabled)
            {
                // بررسی وجود پاسخ معتبر در کش
                if (_memoryCache.TryGetValue(cacheKey, out var cachedResponse) && cachedResponse is BaseApiResponse<Response> validResponse)
                {
                    // اگر استراتژی Enabled باشد یا درخواست تکراری باشد (EnabledOnDuplicateRequest)، از کش استفاده می‌شود
                    if (mizonApiRequest.ClientCacheStrategy == ClientCacheStrategy.Enabled ||
                        (mizonApiRequest.ClientCacheStrategy == ClientCacheStrategy.EnabledOnDuplicateRequest && IsDuplicateRequest(cacheKey)))
                    {
                        return validResponse; // بازگرداندن پاسخ از کش
                    }
                }
            }

            var httpRequestMessage = new HttpRequestMessage()
            {
                Version = HttpVersion.Version30,
            };

            _httpClient.Timeout = mizonApiRequest.TimeoutDuration;

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
                var bytesRequest = Compress(requestJson, mizonApiRequest.CompressionMethod);

                var byteArrayContent = new ByteArrayContent(bytesRequest);
                byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                if (mizonApiRequest.CompressionMethod is CompressionMethod.GZip)
                    byteArrayContent.Headers.ContentEncoding.Add("gzip");

                else if (mizonApiRequest.CompressionMethod is CompressionMethod.Deflate)
                    byteArrayContent.Headers.ContentEncoding.Add("deflate");

                httpRequestMessage.Content = byteArrayContent;
            }
            else throw new NotImplementedException();


            var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage);



            // httpResponseMessage.EnsureSuccessStatusCode();

            var responseJson = await httpResponseMessage.Content.ReadAsStringAsync();


            var response = JsonSerializer.Deserialize<BaseApiResponse<Response>>(responseJson, JsonOptions)!;


            // ذخیره پاسخ در کش در صورت فعال بودن استراتژی کش
            if (mizonApiRequest.ClientCacheStrategy != ClientCacheStrategy.Disabled)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = mizonApiRequest.MaximumClientCacheDuration ?? TimeSpan.FromMinutes(5) // زمان پیش‌فرض برای کش
                };

                _memoryCache.Set(cacheKey, response, cacheEntryOptions);
            }

            return response;
        }
        catch (TaskCanceledException taskCanceledException)
        {
            //baseApiResponse.BaseApiResponseErrorType = BaseApiResponseErrorType.Client;
            //baseApiResponse.Error = new()
            //{
            //    ErrorCode = 2,
            //    ErrorMessage = taskCanceledException.Message
            //};
            return baseApiResponse;
        }
        catch (HttpRequestException httpRequestException)
        {
            //baseApiResponse.BaseApiResponseErrorType = BaseApiResponseErrorType.Client;
            //baseApiResponse.Error = new()
            //{
            //    ErrorCode = 3,
            //    ErrorMessage = httpRequestException.Message
            //};
            return baseApiResponse;
        }
        catch (Exception exception)
        {
            //baseApiResponse.BaseApiResponseErrorType = BaseApiResponseErrorType.Client;
            //baseApiResponse.Error = new()
            //{
            //    ErrorCode = 4,
            //    ErrorMessage = exception.Message
            //};
            return baseApiResponse;
        }
    }

    // تابع برای تولید کلید منحصر به فرد برای کش
    private string GenerateCacheKey<Request, Response>(MizonApiRequest<Request, Response> mizonApiRequest)
        where Request : IApiRequest
        where Response : IApiResponse
    {
        var requestHash = JsonSerializer.Serialize(mizonApiRequest.BaseApiRequest).GetHashCode();
        return $"{mizonApiRequest.Endpoint}_{mizonApiRequest.HttpMethod}_{requestHash}";
    }

    // تابع برای بررسی درخواست تکراری
    private bool IsDuplicateRequest(string cacheKey)
    {
        // بررسی وجود کلید درخواست در کش
        return _memoryCache.TryGetValue(cacheKey, out _);
    }

    private byte[] Compress(string value, CompressionMethod compressionMethod)
    {
        using (var memoryStream = new MemoryStream())
        {
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

            return memoryStream.ToArray();
        }
    }
}