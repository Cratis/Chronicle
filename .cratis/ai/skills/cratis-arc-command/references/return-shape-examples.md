<!-- cratis-ai-managed: skills/cratis-arc-command/references/return-shape-examples.md -->
# Return shape examples

This reference holds worked return-shape examples for a create command and a cross-stream command.

```csharp
[Command]
public record Register<Thing>(<ConceptType> <Name>)
{
    public (<ThingId>, <ThingRegistered>) Handle([GeneratedValue("thing")] <ThingId> id) =>
        (id, new(<Name>));
}
```

```csharp
[Command]
public record Transfer(<AccountId> From, <AccountId> To, <Money> Amount)
{
    public IEnumerable<object> Handle() =>
    [
        new EventForEventSourceId(From, new <Withdrawn>(Amount)),
        new EventForEventSourceId(To, new <Deposited>(Amount)),
    ];
}
```
