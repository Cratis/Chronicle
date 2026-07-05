```csharp
using Cratis.Execution;

public class CorrelationIdentityCausationCorrelation(ICorrelationIdAccessor accessor, ICorrelationIdModifier modifier)
{
    public CorrelationId GetCurrent() => accessor.Current;

    public void SetForRequest() => modifier.Modify(CorrelationId.New());
}
```
