# Tagging

Tags provide a flexible way to organize, categorize, and identify events and observers (reactors, reducers, projections, read models) within Chronicle. By applying the `[Tag]` or `[Tags]` attribute, you can assign one or more tags that describe the purpose, domain, or context of your artifacts.

> **Note**: Both `[Tag]` and `[Tags]` attributes can be used interchangeably. Use whichever feels more natural, or mix them - all tags will be merged together.

## Overview

Tags are strings that can be attached to:

- **Events** - both statically (via attributes) and dynamically (when appending)
- **Reactors** - for organizing and filtering reactive behaviors
- **Reducers** - for categorizing state aggregation logic
- **Projections** - for organizing view models and read models
- **Read Models** - for categorizing data models

Tags are stored with event metadata and observer definitions, enabling powerful filtering, querying, and organizational capabilities.

## Tagging Events

Events can be tagged in two ways: statically using attributes, or dynamically when appending events.

### Static Event Tags

Apply the `[Tag]` or `[Tags]` attribute to your event types to assign static tags that will always be associated with that event type:

```csharp
using Cratis.Chronicle;

[Tag("analytics", "user-action")]
public record UserLoggedIn(string UserId, DateTimeOffset LoggedInAt);

// Or using Tags attribute (plural) for convenience
[Tags("analytics", "user-action")]
public record UserLoggedIn(string UserId, DateTimeOffset LoggedInAt);

// Mix both Tag and Tags attributes
[Tag("security")]
[Tags("audit")]
public record UserPasswordChanged(string UserId, DateTimeOffset ChangedAt);
```

### Dynamic Event Tags

When appending events, you can provide additional tags that will be merged with any static tags:

```csharp
await eventSequence.Append(
    eventSourceId,
    new UserLoggedIn("user123", DateTimeOffset.UtcNow),
    tags: ["production", "critical"]);
```

In this example, the event will have four tags: `["analytics", "user-action", "production", "critical"]`.

### Tags in Event Context

All tags (both static and dynamic) are available on the `EventContext` when processing events:

```csharp
public class UserAnalyticsReducer : IReducerFor<UserAnalytics>
{
    public UserAnalytics On(UserLoggedIn @event, EventContext context)
    {
        // Access tags from the event context
        if (context.Tags.Contains("critical"))
        {
            // Handle critical events differently
        }
        
        return analytics;
    }
}
```

## Tagging Observers

Observers (reactors, reducers, and projections) can be tagged to organize and categorize them:

### Single Tag

```csharp
[Tag("Notifications")]
public class OrderConfirmationReactor { }
```

### Multiple Tags (Single Attribute)

You can use either `[Tag]` or `[Tags]` (plural) for convenience:

```csharp
[Tag("Notifications", "Customer", "Email")]
public class CustomerNotificationReactor { }

// Or using Tags attribute (plural)
[Tags("Notifications", "Customer", "Email")]
public class CustomerNotificationReactor { }
```

### Multiple Tags (Multiple Attributes)

```csharp
[Tag("Integration")]
[Tag("ExternalAPI")]
[Tag("Inventory")]
public class InventorySyncReactor { }
```

### Mixed Approach

You can mix both `[Tag]` and `[Tags]` attributes - all tags will be merged:

```csharp
[Tag("Notifications", "SMS")]
[Tags("Customer")]
public class SmsNotificationReactor { }

// Or mix single and multiple attributes
[Tag("Integration")]
[Tags("ExternalAPI", "Inventory")]
public class InventorySyncReactor { }
```

## Tagging Read Models

Read models and projections can also be tagged:

```csharp
[Tag("Reporting", "Analytics")]
public class SalesReport
{
    public decimal TotalSales { get; set; }
    public int OrderCount { get; set; }
}
```

## Best Practices

- **Use meaningful names**: Choose tag names that clearly describe the purpose or domain
- **Be consistent**: Establish tag naming conventions across your organization
- **Don't over-tag**: Apply only relevant tags; too many can reduce their usefulness
- **Group related artifacts**: Use tags to group events and observers that serve similar purposes
- **Consider hierarchies**: Use dot notation for hierarchical tags (e.g., `"customer.registration"`, `"customer.profile"`)

## Common Tagging Patterns

Here are some common patterns for organizing tags:

### By Domain

```csharp
[Tag("Sales")]
[Tag("Inventory")]
[Tag("Customer")]
[Tag("Shipping")]
```

### By Purpose

```csharp
[Tag("Analytics")]
[Tag("Reporting")]
[Tag("Integration")]
[Tag("Alerting")]
[Tag("Monitoring")]
[Tag("Automation")]
```

### By Integration Type

```csharp
[Tag("Notifications")]
[Tag("ExternalAPI")]
[Tag("MessageQueue")]
[Tag("FileSystem")]
```

### By Communication Channel

```csharp
[Tag("Email")]
[Tag("SMS")]
[Tag("Push")]
[Tag("Webhook")]
```

### By Stakeholder

```csharp
[Tag("Customer")]
[Tag("Operations")]
[Tag("Finance")]
[Tag("Support")]
[Tag("Executive")]
```

### By Environment or Context

```csharp
// Dynamic tags when appending
tags: ["production", "critical"]
tags: ["development", "testing"]
tags: ["migration", "batch-process"]
```

## Using Tags

Tags stored in the event store and observer definitions can be used for:

- **Filtering and searching** for specific events or observers
- **Organizing artifacts** in administrative interfaces
- **Generating documentation** about your system
- **Managing deployments** by tag
- **Monitoring and alerting** based on tag groups
- **Controlling activation** of observers by tag
- **Analyzing event patterns** and flows
- **Creating tag-based subscriptions** or filters

Note: The specific querying and filtering capabilities depend on your Chronicle setup and tooling.
