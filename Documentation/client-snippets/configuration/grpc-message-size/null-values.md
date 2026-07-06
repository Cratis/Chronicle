```csharp
public static class GrpcMessageSizeNullValues
{
    public static ChronicleOptions Create() =>
        new()
        {
            MaxReceiveMessageSize = null, // Uses gRPC default of 4 MB - not recommended
            MaxSendMessageSize = null     // Uses gRPC default of 4 MB - not recommended
        };
}
```
