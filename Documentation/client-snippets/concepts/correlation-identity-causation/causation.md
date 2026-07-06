```csharp
using Cratis.Chronicle.Auditing;

public class CorrelationIdentityCausationCausation(ICausationManager causationManager)
{
    public void RecordPlaceOrder(string orderId) =>
        causationManager.Add(
            "MyApp.Commands.PlaceOrder",
            new Dictionary<string, string> { ["orderId"] = orderId });
}
```
