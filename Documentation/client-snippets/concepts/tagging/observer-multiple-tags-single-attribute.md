```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors;

[Tag("Notifications", "Customer", "Email")]
public class TaggingCustomerNotificationReactor : IReactor;

// [Tags] (plural) is equivalent — use whichever reads more naturally
[Tags("Notifications", "Customer", "Email")]
public class TaggingCustomerNotificationReactorAlternate : IReactor;
```
