```csharp
using Cratis.Chronicle;
using Microsoft.Extensions.Hosting;

public class GetStartedWorker(IEventStore eventStore) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await eventStore.Connection.Connect();

        var bookId = Guid.NewGuid();
        await eventStore.EventLog.Append(bookId, new GetStartedBookAdded("The Pragmatic Programmer", "978-0135957059"));

        // Keep running so reactors and projections keep processing.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
```
