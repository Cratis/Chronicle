```csharp
public static class GrpcMessageSizeConfiguration
{
    public static ChronicleClient Create()
    {
        var options = new ChronicleOptions
        {
            ConnectionString = "chronicle://localhost:35000",
            MaxReceiveMessageSize = 200 * 1024 * 1024, // 200 MB
            MaxSendMessageSize = 200 * 1024 * 1024      // 200 MB
        };

        return new ChronicleClient(options);
    }
}
```
