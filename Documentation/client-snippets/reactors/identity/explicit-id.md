```csharp
using Cratis.Chronicle.Reactors;

// For an already deployed reactor, use its existing stored observer id here.
[Reactor("MyApp.Orders.SendWelcomeEmail")]
public class SendWelcomeEmail : IReactor
{
    // Keep the existing event handlers unchanged.
}
```
