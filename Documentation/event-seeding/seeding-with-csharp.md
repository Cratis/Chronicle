# Seeding with C Sharp

This page shows how to seed events using the Chronicle .NET client. Seeding is sent to the Chronicle Server when the event store connects, and the server applies it once per namespace.

## Define events

Use `record` types for event definitions in examples:

```csharp
public record UserRegistered(string Email, string DisplayName);
public record EmailVerified(string Email);
public record ProfileUpdated(string DisplayName);
public record OrderPlaced(string UserId, decimal Amount);
```

## Implement a seeder

Implement `ICanSeedEvents` and use `IEventSeedingBuilder` to define events to append:

```csharp
public sealed class UserSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder
            .For<UserRegistered>("user-123", [
                new("john@example.com", "John")
            ])
            .ForEventSource("user-456", [
                new UserRegistered("jane@example.com", "Jane"),
                new EmailVerified("jane@example.com")
            ]);
    }
}
```

## Seed multiple events of the same type

```csharp
builder.For<UserRegistered>("user-123", [
    new("john@example.com", "John"),
    new("jane@example.com", "Jane")
]);
```

## Seed mixed event types

```csharp
builder.ForEventSource("user-123", [
    new UserRegistered("john@example.com", "John"),
    new EmailVerified("john@example.com"),
    new ProfileUpdated("John Doe")
]);
```

## Development-only seeding

If seed data should only run in development, use conditional compilation or runtime configuration:

```csharp
#if DEBUG
public sealed class DevelopmentSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder.For<UserRegistered>("dev-user-1", [
            new("dev@example.com", "Dev User")
        ]);
    }
}
#endif
```

Chronicle does not distinguish between development and production seed data. Decide when to seed based on build configuration or runtime settings.

## Organize seeders by feature

For larger solutions, split seeders by domain or feature:

```csharp
public sealed class UserDevelopmentSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder.For<UserRegistered>("test-user-1", [
            new("test1@example.com", "Test User 1")
        ]);
    }
}

public sealed class OrderDevelopmentSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder.For<OrderPlaced>("test-order-1", [
            new("test-user-1", 100.00m)
        ]);
    }
}
```

## How it runs

- Seeders are automatically discovered at application startup.
- Seed batches are sent to the Chronicle Server when the event store connects.
- The server deduplicates seeded events and applies them once per namespace.
- Events are appended in a single batch for efficient startup.

## Best practices

- Keep seed data minimal and deterministic.
- Use clear event source IDs to make debugging easier.
- Group seeders by scenario so you can remove or adjust them easily.
- Use build flags or runtime settings to prevent seeding in production.

