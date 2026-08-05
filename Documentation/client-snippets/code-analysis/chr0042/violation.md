```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record Chr0042SideReleasedForSigning(Guid RequestId);

[EventType]
public record Chr0042RequestLost(bool IsContractRoundLive);

// Warning CHR0042: Property 'IsContractRoundLive' is written both locally by
// [SetValue<Chr0042SideReleasedForSigning>] and by the join with 'Chr0042RequestLost'.
// The re-release writes true, but the join re-applies false on top — the latch
// silently sticks and the row is withheld forever.
[FromEvent<Chr0042SideReleasedForSigning>]
public record Chr0042ContractSide(
    [Key] Guid Id,
    Guid RequestId,
    [SetValue<Chr0042SideReleasedForSigning>(true)]
    [Join<Chr0042RequestLost>(on: "RequestId")]
    bool IsContractRoundLive);
```
