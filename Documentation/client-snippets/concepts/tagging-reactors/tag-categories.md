```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors;

// By integration type
[Tag("Notifications", "ExternalAPI", "MessageQueue", "FileSystem")]
// By domain
[Tag("Sales", "Inventory", "Customer", "Shipping")]
// By communication channel
[Tag("Email", "SMS", "Push", "Webhook")]
// By purpose
[Tag("Integration", "Alerting", "Monitoring", "Automation")]
// By stakeholder
[Tag("Customer", "Operations", "Finance", "Support")]
public class TaggingReactorsCategoryExamplesReactor : IReactor;
```
