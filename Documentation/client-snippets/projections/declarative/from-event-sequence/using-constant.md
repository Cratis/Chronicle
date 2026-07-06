```csharp
public static class DecFromEventSequenceEventSequences
{
    public const string OrderManagement = "order-management";
}

public class DecFromEventSequenceOrderProjectionWithConstant : IProjectionFor<DecFromEventSequenceOrder>
{
    public void Define(IProjectionBuilderFor<DecFromEventSequenceOrder> builder) => builder
        // Using a constant instead of a raw string keeps the sequence identifier consistent
        // wherever it is referenced.
        .FromEventSequence(DecFromEventSequenceEventSequences.OrderManagement)
        .AutoMap()
        .From<DecFromEventSequenceOrderCreated>();
}
```
