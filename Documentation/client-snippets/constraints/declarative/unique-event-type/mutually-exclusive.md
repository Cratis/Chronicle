```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record ConstraintsPersonAliasedTo(Guid Target);

[EventType]
public record ConstraintsPersonErased;

public class ConstraintsPersonTerminalOutcome : IConstraint
{
    // Both declarations share one constraint name, so they become a single constraint:
    // at most one event drawn from { ConstraintsPersonAliasedTo, ConstraintsPersonErased }
    // per person. A person merged away can no longer be erased, and neither event can
    // occur twice.
    public void Define(IConstraintBuilder builder)
    {
        builder.Unique<ConstraintsPersonAliasedTo>(
            name: "PersonTerminal",
            message: "This person already has a terminal outcome.");

        builder.Unique<ConstraintsPersonErased>(
            name: "PersonTerminal",
            message: "This person already has a terminal outcome.");
    }
}
```
