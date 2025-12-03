# Constraints

Constraints help maintain data integrity by preventing invalid events from being appended to the event log.
They run at the database level within Chronicle, ensuring consistency across your event sources.

## Unique Event Type Constraint

The simplest constraint ensures only one instance of a specific event type can exist per event source.
This is useful when you want to prevent duplicate events like "user registration" or "account creation".

```csharp
using Cratis.Chronicle.Events.Constraints;

[EventType]
[Unique]
public record UserRegistered(UserName Name, EmailAddress Email);
```

The `[Unique]` attribute automatically creates a unique event type constraint, preventing multiple `UserRegistered` events for the same event source.

## Unique Property Constraint

For more granular control, you can constrain on specific properties across multiple event types.
This is useful when different events modify the same unique value, like a username.

```csharp
using Cratis.Chronicle.Events.Constraints;

public class UserConstraints : IConstraint
{
    public void Define(IConstraintBuilder builder)
    {
        builder.Unique(unique => unique
            .On<UserRegistered>(e => e.Name)
            .On<UserNameChanged>(e => e.NewName)
            .WithName("UniqueUserName"));
    }
}
```

This constraint ensures that the `Name` property in `UserRegistered` and `NewName` property in `UserNameChanged` remain unique across all event sources.

## Constraint with Custom Message

You can provide custom violation messages to make errors more meaningful:

```csharp
public class UserConstraints : IConstraint
{
    public void Define(IConstraintBuilder builder)
    {
        builder.Unique<UserRegistered>(
            message: "A user with this information is already registered.",
            name: "UniqueUser");
    }
}
```

All constraints are automatically discovered at runtime and enforced when appending events to the event log.
