# Exceptions, Logging, and Dependency Injection — Reference

## Exceptions

### Rules

- Only throw for truly exceptional situations — never for control flow
- Always create a **custom** exception type — never use built-in types (`InvalidOperationException`, `ArgumentException`, etc.)
- **No `Exception` suffix** — `AuthorNotFound` reads better than `AuthorNotFoundException`
- Provide a meaningful message
- Add XML `<exception>` doc starting with "The exception that is thrown when ..."

### Pattern

```csharp
/// <summary>
/// The exception that is thrown when an author is not found.
/// </summary>
/// <param name="id">The <see cref="AuthorId"/> that was not found.</param>
public class AuthorNotFound(AuthorId id) : Exception($"Author with id '{id}' was not found");
```

Usage:

```csharp
var author = await _authors.FindById(id)
    ?? throw new AuthorNotFound(id);
```

---

## Logging

### Rules

- Structured logging with named parameters
- `ILogger<T>` where `T` is the containing class
- Log message definitions go in a separate `<ClassName>Logging.cs` file — partial static internal class
- Use `[LoggerMessage]` attribute — do **not** include `eventId`
- Appropriate log levels: `Information`, `Warning`, `Error`, `Debug`

### Logging class pattern

```csharp
// AuthorServiceLogging.cs
namespace MyApp.Authors;

static partial class AuthorServiceLogging
{
    [LoggerMessage(LogLevel.Information, "Registering author '{Name}'")]
    internal static partial void RegisteringAuthor(this ILogger<AuthorService> logger, AuthorName name);

    [LoggerMessage(LogLevel.Warning, "Author with name '{Name}' already exists")]
    internal static partial void AuthorAlreadyExists(this ILogger<AuthorService> logger, AuthorName name);
}
```

Usage in the service:

```csharp
public class AuthorService(ILogger<AuthorService> logger)
{
    public Task Register(AuthorName name)
    {
        logger.RegisteringAuthor(name);
        // ...
    }
}
```

---

## Dependency Injection

### Rules

- Prefer **constructor injection** — never use `IServiceProvider` directly (service locator anti-pattern)
- For singletons, use the `[Singleton]` attribute — no explicit `services.AddSingleton<>()` needed
- Convention-based systems (`IFoo → Foo`) are auto-discovered — don't register them explicitly
- `Handle()` method parameters are automatically resolved from DI — no manual wiring needed

### [Singleton] attribute

```csharp
[Singleton]
public class AuthorService(IEventLog eventLog) : IAuthorService
{
    // registered as singleton automatically
}
```

### DI in Handle()

```csharp
[Command]
public record SendNotification(AuthorId AuthorId)
{
    // INotificationService resolved from DI automatically
    public async Task Handle(INotificationService notifications) =>
        await notifications.Notify(AuthorId, "Welcome!");
}
```

### Avoiding service locator

```csharp
// ✅ Constructor injection
public class MyService(IAuthorService authors) { ... }

// ❌ Service locator
public class MyService(IServiceProvider provider)
{
    void DoWork() => provider.GetService<IAuthorService>()!.DoSomething();
}
```
