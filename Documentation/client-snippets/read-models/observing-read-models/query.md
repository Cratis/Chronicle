```csharp
using Cratis.Chronicle.ReadModels;

using Reactive = System.Reactive.Subjects;

// Add these query methods to a read model maintained by a projection or reducer.
public record LiveOrder(string OrderId, OrderStatus Status)
{
    public static Reactive.ISubject<IEnumerable<LiveOrder>> All(IReadModels readModels) =>
        readModels.Observe<LiveOrder>();

    public static Reactive.ISubject<IEnumerable<LiveOrder>> ByStatus(
        OrderStatus status, IReadModels readModels) =>
        readModels.Observe<LiveOrder>(order => order.Status == status);
}
```
