# Constant Keys

A constant key allows all events of a given type to accumulate into a **single** read model instance, regardless of which event source the events originate from. This is useful for global aggregates, system-wide counters, and singleton read models.

## Defining a constant key

Use `UsingConstantKey(string value)` to specify a fixed key value for a `from` block:

```csharp
public class GlobalCounterProjection : IProjectionFor<GlobalCounter>
{
    public void Define(IProjectionBuilderFor<GlobalCounter> builder) => builder
        .From<OrderPlaced>(_ => _
            .UsingConstantKey("global")
            .Count(m => m.TotalOrders));
}
```

Every `OrderPlaced` event, from every event source, updates the single `GlobalCounter` instance with the key `"global"`.

## Read model

The read model is a normal record or class. The constant key becomes its `_id` in the underlying store:

```csharp
public record GlobalCounter(int TotalOrders);
```

## Combining with functions

Constant keys work with all counting and arithmetic functions, making them ideal for global aggregates:

```csharp
public class SiteStatisticsProjection : IProjectionFor<SiteStatistics>
{
    public void Define(IProjectionBuilderFor<SiteStatistics> builder) => builder
        .From<UserRegistered>(_ => _
            .UsingConstantKey("site")
            .Count(m => m.TotalUsers))
        .From<UserLoggedIn>(_ => _
            .UsingConstantKey("site")
            .Increment(m => m.ActiveSessions))
        .From<UserLoggedOut>(_ => _
            .UsingConstantKey("site")
            .Decrement(m => m.ActiveSessions));
}
```

```csharp
public record SiteStatistics(
    int TotalUsers,
    int ActiveSessions);
```

## Constant parent keys

Use `UsingConstantParentKey(string value)` when working with child collections and you want all events to target the same parent read model:

```csharp
public class TeamActivityProjection : IProjectionFor<Team>
{
    public void Define(IProjectionBuilderFor<Team> builder) => builder
        .Children(m => m.Members, children => children
            .IdentifiedBy(e => e.UserId)
            .From<UserJoined>(_ => _
                .UsingConstantParentKey("main-team")
                .Set(m => m.Name).To(e => e.UserName)));
}
```

## Comparison to other key strategies

| Strategy | Method | When to use |
|---|---|---|
| Event source ID | (default) | Each event stream is one instance |
| Event property | `UsingKey(e => e.Property)` | Property on the event identifies instance |
| Event context | `UsingKeyFromContext(c => c.Property)` | Event context property identifies instance |
| Composite | `UsingCompositeKey<T>(...)` | Multiple values together form identity |
| **Constant** | **`UsingConstantKey("value")`** | **All events update the same instance** |

## Full example

```csharp
using Cratis.Chronicle.Projections;

[EventType]
public record PageViewed(string PageUrl);

[EventType]
public record ButtonClicked(string ButtonId);

[EventType]
public record FormSubmitted(string FormId);

public record EngagementMetrics(
    int PageViews,
    int ButtonClicks,
    int FormSubmissions);

public class EngagementMetricsProjection : IProjectionFor<EngagementMetrics>
{
    public void Define(IProjectionBuilderFor<EngagementMetrics> builder) => builder
        .From<PageViewed>(_ => _
            .UsingConstantKey("metrics")
            .Count(m => m.PageViews))
        .From<ButtonClicked>(_ => _
            .UsingConstantKey("metrics")
            .Count(m => m.ButtonClicks))
        .From<FormSubmitted>(_ => _
            .UsingConstantKey("metrics")
            .Count(m => m.FormSubmissions));
}
```

This projection collects engagement events from all users and event sources into a single `EngagementMetrics` document with the key `"metrics"`.
