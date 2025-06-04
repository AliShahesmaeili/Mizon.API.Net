namespace Mizon.API.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class RealtimeEntityAttribute : Attribute
{
    public string GroupPrefix { get; }
    public string SubscribeMethod { get; }
    public string UnsubscribeMethod { get; }

    public RealtimeEntityAttribute(string groupPrefix, string subscribeMethod, string unsubscribeMethod)
    {
        GroupPrefix = groupPrefix;
        SubscribeMethod = subscribeMethod;
        UnsubscribeMethod = unsubscribeMethod;
    }
}
