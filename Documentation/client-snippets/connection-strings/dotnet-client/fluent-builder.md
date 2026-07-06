```csharp
using Cratis.Chronicle.Connections;

public static class ConnectionStringsFluentBuilder
{
    public static ChronicleClient Create()
    {
        var connectionString = new ChronicleConnectionStringBuilder()
            .WithHost("server.example.com")
            .WithPort(35000)
            .WithCredentials("clientId", "clientSecret")
            .Build();

        var options = ChronicleOptions.FromConnectionString(connectionString);
        return new ChronicleClient(options);
    }
}
```
