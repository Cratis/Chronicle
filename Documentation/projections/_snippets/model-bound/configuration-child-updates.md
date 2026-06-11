```csharp
public record Dashboard(
    [Key] Guid Id,
    string Name,

    [ChildrenFrom<ConfigurationAdded>(
        key: nameof(ConfigurationAdded.ConfigurationId),
        parentKey: nameof(ConfigurationAdded.DashboardId))]
    IEnumerable<Configuration> Configurations);

[FromEvent<ConfigurationRenamed>(parentKey: nameof(ConfigurationRenamed.DashboardId))]
public record Configuration(
    [Key] Guid Id,
    string Name);
```
