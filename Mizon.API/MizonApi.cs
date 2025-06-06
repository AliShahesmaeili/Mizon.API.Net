﻿using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Mizon.API;


public interface IMizonApiResponseMiddleware
{
    Task InvokeAsync(IBaseApiRequest request, IBaseApiResponse response);
}

public class MizonApi
{

    private readonly List<IMizonApiResponseMiddleware> _responseMiddlewares = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly HubConnection _hubConnection;

    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IMemoryCache _memoryCache;
    private string? _token;

    public MizonApi(HttpClient httpClient,
        IMemoryCache memoryCache,
        IServiceProvider serviceProvider,
        HubConnection hubConnection)
    {
        _httpClient = httpClient;
        _memoryCache = memoryCache;
        _serviceProvider = serviceProvider;
        _hubConnection = hubConnection;
    }

    public void SetToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be null or empty.", nameof(token));

        _token = token;
    }

    public string? GetToken() => _token;


    public void UseResponseMiddleware<T>() where T : class, IMizonApiResponseMiddleware
    {
        var instance = _serviceProvider.GetService(typeof(T)) as IMizonApiResponseMiddleware
                       ?? throw new InvalidOperationException($"Unable to resolve response middleware '{typeof(T).Name}' from DI container.");
        _responseMiddlewares.Add(instance);
    }




    public async Task<BaseApiResponse<Response>> SendRequestAsync<Request, Response>(MizonApiRequest<Request, Response> mizonApiRequest, CancellationToken? cancellationToken = null)
        where Request : IApiRequest where Response : IApiResponse
    {
        var baseApiResponse = new BaseApiResponse<Response>(_hubConnection);

        try
        {
            var cacheKey = ManageCacheKey(mizonApiRequest.BaseApiRequest, mizonApiRequest.Endpoint, mizonApiRequest.HttpMethod);

            var cachedData = ManageGetCache(cacheKey, mizonApiRequest.ClientCacheStrategy);

            if (cachedData is BaseApiResponse<Response> cachedResponse)
            {
                cachedResponse.IsFromCache = true;
                return cachedResponse;
            }

            var httpRequestMessage = new HttpRequestMessage();

            ManageHttpVersion(httpRequestMessage);

            ManageAuthorization(httpRequestMessage, mizonApiRequest.NeedAuthorized, _token);



            if (mizonApiRequest.HttpMethod == HttpMethod.GET)
            {
                //var queryString = GetQueryStringFromObject(mizonApiRequest.BaseApiRequest);
                httpRequestMessage.RequestUri = new($"{mizonApiRequest.Endpoint}");
                httpRequestMessage.Method = System.Net.Http.HttpMethod.Get;
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
            var responseJson = await httpResponseMessage.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<BaseApiResponse<Response>>(responseJson, JsonOptions);

            ManageToken(mizonApiRequest.PropertyForToken, response);
            ManageSetCache(cacheKey, response, mizonApiRequest.ClientCacheStrategy, mizonApiRequest.MaximumClientCacheDuration);

            // Run response middleware
            foreach (var middleware in _responseMiddlewares)
            {
                await middleware.InvokeAsync(mizonApiRequest.BaseApiRequest, response);
            }

            return response!;
        }
        catch (SocketException ex)
        {
            baseApiResponse.Error = new BaseApiError
            {
                Code = 101,
                Title = "Network Error",
                Details = "A socket error occurred while trying to send the request. " + ex.Message
            };
        }
        catch (IOException ex)
        {
            baseApiResponse.Error = new BaseApiError
            {
                Code = 102,
                Title = "I/O Error",
                Details = "An I/O error occurred during request processing. " + ex.Message
            };
        }
        catch (JsonException ex)
        {
            baseApiResponse.Error = new BaseApiError
            {
                Code = 103,
                Title = "Invalid JSON",
                Details = "Failed to parse the JSON response. " + ex.Message
            };
        }
        catch (TimeoutException ex)
        {
            baseApiResponse.Error = new BaseApiError
            {
                Code = 104,
                Title = "Timeout",
                Details = "The request operation timed out. " + ex.Message
            };
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            baseApiResponse.Error = new BaseApiError
            {
                Code = 105,
                Title = "Cancelled by User",
                Details = "The request was cancelled by the caller. " + ex.Message
            };
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            baseApiResponse.Error = new BaseApiError
            {
                Code = 106,
                Title = "Request Timeout",
                Details = "The request was automatically cancelled due to timeout. " + ex.Message
            };
        }
        catch (HttpRequestException ex)
        {
            baseApiResponse.Error = new BaseApiError
            {
                Code = 107,
                Title = "HTTP Error",
                Details = "An HTTP error occurred while sending the request. " + ex.Message
            };
        }
        catch (Exception ex)
        {
            baseApiResponse.Error = new BaseApiError
            {
                Code = 108,
                Title = "Unexpected Error",
                Details = "An unexpected error occurred. " + ex.Message
            };
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

        if (baseApiResponse.ResponseContent is null)
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