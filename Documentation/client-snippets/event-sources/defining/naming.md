```csharp
using Cratis.Chronicle.EventSources;

// The event source type is "Loan": an explicit name wins.
[EventSource("Loan")]
public class BankLoanDefinition : IEventSource;

// The event source type is "ShoppingCart": the "EventSource" suffix is removed from the type name.
[EventSource(Description = "A customer's shopping cart")]
public class ShoppingCartEventSource : IEventSource;

// The attribute is optional. The event source type is "Invoice", with no description and no concurrency dimensions.
public class InvoiceEventSource : IEventSource;
```
