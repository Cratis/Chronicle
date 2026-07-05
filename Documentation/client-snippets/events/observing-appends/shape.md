```csharp
using Cratis.Chronicle.EventSequences;

public interface IObservingAppendsEventSequence
{
    IObservable<IEnumerable<AppendedEventWithResult>> AppendOperations { get; }
}
```
