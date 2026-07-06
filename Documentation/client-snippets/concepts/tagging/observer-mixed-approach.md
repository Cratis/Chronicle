```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors;

[Tag("Notifications", "SMS")]
[Tags("Customer")]
public class TaggingSmsNotificationReactor : IReactor;

// Or mix single and multiple attributes the other way around
[Tag("Integration")]
[Tags("ExternalAPI", "Inventory")]
public class TaggingInventorySyncReactorMixed : IReactor;
```
