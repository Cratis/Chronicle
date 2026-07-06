```csharp
[EventType]
public record MbChildrenRemovedFeatureActivated(Guid FeatureId, string Name);

[EventType]
public record MbChildrenRemovedFeatureDeactivated(Guid FeatureId);

public record MbChildrenRemovedSubscription(
    [Key]
    Guid SubscriptionId,

    [ChildrenFrom<MbChildrenRemovedFeatureActivated>(key: nameof(MbChildrenRemovedFeatureActivated.FeatureId))]
    [RemovedWithJoin<MbChildrenRemovedFeatureDeactivated>(key: nameof(MbChildrenRemovedFeatureDeactivated.FeatureId))]
    IEnumerable<MbChildrenRemovedFeature> Features);

public record MbChildrenRemovedFeature(
    [Key] Guid FeatureId,
    string Name);
```
