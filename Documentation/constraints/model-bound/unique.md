# Unique property constraint

Adorn a property with `[Unique]` to enforce that the **value of that property is unique** across every event of this type in the event store.

Chronicle discovers `[Unique]`-adorned properties automatically and registers the constraints with the Kernel when the client starts.

## Defining a unique property constraint

```csharp
[EventType]
public record ProjectCreated([Unique] string Name, string Description);
```

## Grouping constraints across event types

When multiple event types share the same constraint `name`, Chronicle groups them into a single constraint. A value introduced by any of the participating events is checked against all others:

```csharp
[EventType]
public record UserRegistered([Unique(name: "UniqueEmail")] string Email, string DisplayName);

[EventType]
public record UserEmailChanged([Unique(name: "UniqueEmail")] string NewEmail);
```

Both events now participate in the same `UniqueEmail` constraint, so neither `UserRegistered` nor `UserEmailChanged` can introduce an email address that already exists.

## Violation message

The `message` parameter is optional. Chronicle produces a default violation message when one is not supplied:

```csharp
[EventType]
public record ProjectCreated([Unique(message: "A project with this name already exists.")] string Name, string Description);
```

## Releasing a constraint

Apply `[RemoveConstraint]` to the event type that signals a domain object has been removed. When this event is appended, Chronicle releases the named constraint and its previously held values can be claimed again:

```csharp
[EventType]
[RemoveConstraint("UniqueEmail")]
public record UserRemoved(UserId UserId);
```

The constraint name must exactly match the name used in the `[Unique]` attribute that established it.

An event type can release more than one constraint by stacking multiple attributes:

```csharp
[EventType]
[RemoveConstraint("UniqueEmail")]
[RemoveConstraint("UniqueUsername")]
public record UserRemoved(UserId UserId);
```

## How constraints are enforced

The Chronicle Kernel reads the `[Unique]` and `[RemoveConstraint]` attributes when the client connects and creates the indexes it needs. Every subsequent append is checked against those indexes server-side, so no constraint logic runs in client code.
