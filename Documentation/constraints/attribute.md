# [Unique] attribute

The `[Unique]` attribute is the preferred way to declare uniqueness constraints on event types. Adorn the event type class or one of its properties directly — no separate constraint class is needed.

Chronicle discovers `[Unique]`-adorned types automatically and registers the constraints with the Kernel when the client starts.

## Event-type uniqueness

Adorn the event type class with `[Unique]` to enforce that only **one event of this type** can ever be appended per event source. This is the right choice when the event itself is a unique fact — for example, registering a user.

```csharp
[EventType]
[Unique]
public record UserRegistered(string Email, string DisplayName);
```

An optional constraint name and violation message can be provided:

```csharp
[EventType]
[Unique(name: "UniqueUser", message: "A user with this identity has already been registered.")]
public record UserRegistered(string Email, string DisplayName);
```

## Property uniqueness

Adorn a specific property with `[Unique]` to enforce that the **value of that property is unique across every event of this type** for a given event source.

```csharp
[EventType]
public record ProjectCreated([Unique] string Name, string Description);
```

### Grouping constraints across event types

When multiple event types share the same constraint `name`, Chronicle groups them into a single constraint. A value introduced by any of the participating events is checked against all others.

```csharp
[EventType]
public record UserRegistered([Unique(name: "UniqueEmail")] string Email, string DisplayName);

[EventType]
public record UserEmailChanged([Unique(name: "UniqueEmail")] string NewEmail);
```

Both events now participate in the same `UniqueEmail` constraint, so neither `UserRegistered` nor `UserEmailChanged` can introduce an email address that already exists in either stream.

## Violation message

The `message` parameter is optional. Chronicle produces a default violation message when one is not supplied.

```csharp
[EventType]
[Unique(message: "A project with this name already exists.")]
public record ProjectCreated(string Name);
```

## Releasing a constraint with [RemoveConstraint]

Apply `[RemoveConstraint]` to the event type that signals a domain object has been removed. When this event is appended, Chronicle releases the named constraint and its previously held values can be claimed again.

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
