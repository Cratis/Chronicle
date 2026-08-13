```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;

[EventType]
public record Chr0046UserSignedUp(string Hash, string Region);

// Warning CHR0046: 'UsingKey' keys the 'Chr0046UserByHash' document by 'Hash', but
// 'Chr0046UserByHash' is passive. The events were appended to the user's own stream, so a read
// by hash replays the hash stream, reaches nothing, and hands back a default-initialized model.
[Passive]
public record Chr0046UserByHash(
    [Key] string Id,
    string Region);

public class Chr0046UserByHashProjection : IProjectionFor<Chr0046UserByHash>
{
    public void Define(IProjectionBuilderFor<Chr0046UserByHash> builder) => builder
        .From<Chr0046UserSignedUp>(_ => _
            .UsingKey(e => e.Hash)
            .Set(m => m.Region).To(e => e.Region));
}

// The same redirection on a read model that keeps its sink is not reported: an observer
// materializes the redirected document, and the read is answered from storage.
public record Chr0046UserByHashMaterialized(
    [Key] string Id,
    string Region);

public class Chr0046UserByHashMaterializedProjection : IProjectionFor<Chr0046UserByHashMaterialized>
{
    public void Define(IProjectionBuilderFor<Chr0046UserByHashMaterialized> builder) => builder
        .From<Chr0046UserSignedUp>(_ => _
            .UsingKey(e => e.Hash)
            .Set(m => m.Region).To(e => e.Region));
}
```
