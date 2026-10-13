```csharp
using System.Reactive.Linq;
using Cratis.Chronicle.ReadModels;

public static class ConfirmedOrderSubscription
{
    public static IDisposable Subscribe(IReadModels readModels) => readModels
        .Observe<Order>(order => order.Status == OrderStatus.Confirmed)
        .Subscribe(
            orders => Console.WriteLine($"Confirmed orders: {orders.Count()}"),
            error => Console.WriteLine(error.Message));
}
```
