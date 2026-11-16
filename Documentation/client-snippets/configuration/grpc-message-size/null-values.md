```csharp
public static class GrpcMessageSizeNullValues
{
    public static ChronicleOptions Create() =>
        new()
        {
            MaxReceiveMessageSize = null, // No client-side limit - not recommended
            MaxSendMessageSize = null     // No client-side limit - not recommended
        };
}
```
