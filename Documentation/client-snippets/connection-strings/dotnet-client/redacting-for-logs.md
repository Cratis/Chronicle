```csharp
using Cratis.Chronicle.Connections;

public static class ConnectionStringsRedactingForLogs
{
    public static void LogConnectionTarget(ILogger logger)
    {
        var connectionString = new ChronicleConnectionString("chronicle://clientId:clientSecret@server.example.com:35000");

        // Logs: chronicle://clientId:***@server.example.com:35000
        logger.LogInformation("Connecting to {RedactedConnectionString}", connectionString.Redacted);
    }
}
```
