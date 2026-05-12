# Unique event type constraint

Adorn the event type class with `[Unique]` to enforce that **only one event of this type** can be appended per event source. Use this when the event itself is a unique fact — for example, registering a user can only happen once.

Chronicle discovers `[Unique]`-adorned types automatically and registers the constraints with the Kernel when the client starts.

## Defining a unique event type constraint

```csharp
[EventType]
[Unique]
public record UserRegistered(string Email, string DisplayName);
```

## Constraint name and violation message

An optional constraint name and violation message can be provided:

```csharp
[EventType]
[Unique(name: "UniqueUser", message: "A user with this identity has already been registered.")]
public record UserRegistered(string Email, string DisplayName);
```

The `message` parameter is optional. Chronicle produces a default violation message when one is not supplied.

## Releasing a constraint

Apply `[RemoveConstraint]` to the event type that signals a domain object has been removed. When this event is appended, Chronicle releases the constraint, and the event source can accept a new event of this type:

```csharp
[EventType]
[RemoveConstraint("UniqueUser")]
public record UserRemoved(UserId UserId);
```

The constraint name must exactly match the name provided to the `[Unique]` attribute. When no name was provided to `[Unique]`, Chronicle uses the event type name as the default constraint name.

## How constraints are enforced

The Chronicle Kernel reads the `[Unique]` and `[RemoveConstraint]` attributes when the client connects and creates the indexes it needs. Every subsequent append is checked against those indexes server-side, so no constraint logic runs in client code.
