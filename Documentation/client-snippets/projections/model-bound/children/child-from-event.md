```csharp
[EventType]
public record MbChildrenChildFromEventConfigurationAdded(Guid DashboardId, Guid ConfigurationId, string Name);

[EventType]
public record MbChildrenChildFromEventConfigurationRenamed(Guid DashboardId, Guid Id, string Name);

public record MbChildrenChildFromEventDashboard(
    [Key] Guid Id,
    string Name,

    [ChildrenFrom<MbChildrenChildFromEventConfigurationAdded>(
        key: nameof(MbChildrenChildFromEventConfigurationAdded.ConfigurationId),
        parentKey: nameof(MbChildrenChildFromEventConfigurationAdded.DashboardId))]
    IEnumerable<MbChildrenChildFromEventConfiguration> Configurations);

[FromEvent<MbChildrenChildFromEventConfigurationRenamed>(parentKey: nameof(MbChildrenChildFromEventConfigurationRenamed.DashboardId))]
public record MbChildrenChildFromEventConfiguration(
    [Key] Guid Id,
    string Name);
```
