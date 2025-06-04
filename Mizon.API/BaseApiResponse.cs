using Microsoft.AspNetCore.SignalR.Client;
using Mizon.API.Attributes;
using System.Reflection;

namespace Mizon.API;

public interface IBaseApiResponse { }


/// <summary>
/// BaseApiResponse is a generic class representing the response from an API call.
/// It can either contain the successful response data or an error.
/// </summary>
/// <typeparam name="TApiResponse">The type of the successful API response data, which must implement the <see cref="IApiResponse"/> interface.</typeparam>
public class BaseApiResponse<TApiResponse> : IBaseApiResponse
    where TApiResponse : IApiResponse
{

    private readonly HubConnection? _hubConnection;

    public BaseApiResponse(HubConnection? hubConnection = null)
    {
        _hubConnection = hubConnection;
    }


    /// <summary>
    /// The successful API response data, or null if an error occurred.
    /// </summary>
    public TApiResponse? ResponseContent { get; set; }


    /// <summary>
    /// Indicates whether the operation was successful.
    /// This property returns `true` if the operation completed without any errors,
    /// </summary>
    public bool IsSuccess { get => Error == null; }


    /// <summary>
    /// The error that occurred during the API call, or null if the call was successful.
    /// </summary>
    public BaseApiError? Error { get; set; }


    /// <summary>
    /// Indicates whether the response was retrieved from cache.
    /// </summary>
    public bool IsFromCache { get; set; } = false;


    private bool _subscribed = false;

    public event Action<string, object?>? OnPropertyUpdated;

    private string? _entityId;
    private readonly List<string> _registeredEvents = new();


    public async Task SubscribeToRealtimeUpdates()
    {
        if (_subscribed || ResponseContent == null) return;

        var type = typeof(TApiResponse);

        var entityAttr = type.GetCustomAttribute<RealtimeEntityAttribute>()
            ?? throw new InvalidOperationException($"Missing [RealtimeEntity] attribute on {type.Name}");

        var idProp = type.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<RealtimeEntityIdAttribute>() != null);

        _entityId = idProp?.GetValue(ResponseContent)?.ToString();
        if (string.IsNullOrEmpty(_entityId))
            throw new InvalidOperationException("Cannot determine entity ID for realtime subscription.");

        var updatableProps = type
            .GetProperties()
            .Where(p => p.GetCustomAttribute<RealtimeUpdatableAttribute>() != null)
            .ToList();

        foreach (var prop in updatableProps)
        {
            var eventName = prop.Name;
            _registeredEvents.Add(eventName);

            _hubConnection.On<string, object>(eventName, (id, newValue) =>
            {
                if (id != _entityId) return;

                var converted = Convert.ChangeType(newValue, prop.PropertyType);
                prop.SetValue(ResponseContent, converted);
                OnPropertyUpdated?.Invoke(prop.Name, converted);
            });
        }

        await _hubConnection.InvokeAsync(entityAttr.SubscribeMethod, $"{entityAttr.GroupPrefix}_{_entityId}");

        _subscribed = true;
    }

    public async Task UnsubscribeFromRealtimeUpdates()
    {
        if (!_subscribed || _hubConnection == null || string.IsNullOrEmpty(_entityId)) return;

        var type = typeof(TApiResponse);
        var entityAttr = type.GetCustomAttribute<RealtimeEntityAttribute>()
            ?? throw new InvalidOperationException($"Missing [RealtimeEntity] attribute on {type.Name}");

        foreach (var eventName in _registeredEvents)
        {
            _hubConnection.Remove(eventName);
        }

        await _hubConnection.InvokeAsync(entityAttr.UnsubscribeMethod, $"{entityAttr.GroupPrefix}_{_entityId}");

        _registeredEvents.Clear();
        _entityId = null;
        _subscribed = false;
    }
}
