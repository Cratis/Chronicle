```csharp
using Cratis.Chronicle.EventSequences;

public class ObservingAppendsMonitor(IEventLog eventLog) : IDisposable
{
    readonly IDisposable _subscription = eventLog.AppendOperations.Subscribe(OnAppended);

    static void OnAppended(IEnumerable<AppendedEventWithResult> operations)
    {
        foreach (var item in operations)
        {
            Console.WriteLine($"Event {item.Event.Content.GetType().Name} appended: success={item.Result.IsSuccess}");
        }
    }

    public void Dispose() => _subscription.Dispose();
}
```
