```csharp
using System.Reactive.Linq;
using Cratis.Chronicle.ReadModels;

public static class OrderListSubscription
{
    // The caller disposes this subscription when the list is no longer needed.
    public static IDisposable Subscribe(IReadModels readModels) => readModels
        .Observe<Order>()
        .Subscribe(
            orders => Console.WriteLine($"Current orders: {orders.Count()}"),
            error => Console.WriteLine(error.Message));
}
```
