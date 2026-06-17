# Code Style — Reference

## Records

Use `record` types for all immutable data structures — events, commands, read models, concepts, DTOs.

```csharp
// ✅ Prefer record
public record AuthorRegistered(AuthorName Name);

// ✅ Record with primary constructor
public record Author(AuthorId Id, AuthorName Name);
```

Records give value equality, immutability, and concise syntax for free. A `record class` with `init`-only properties is equivalent when you need extra methods.

---

## Primary Constructors

Use primary constructors for all types — they eliminate the field declaration + constructor ceremony.

```csharp
// ✅ Primary constructor
public class AuthorService(IEventLog eventLog, ILogger<AuthorService> logger)
{
    public async Task Register(AuthorName name) =>
        await eventLog.Append(AuthorId.New(), new AuthorRegistered(name));
}

// ❌ Verbose constructor + fields
public class AuthorService
{
    readonly IEventLog _eventLog;
    readonly ILogger<AuthorService> _logger;

    public AuthorService(IEventLog eventLog, ILogger<AuthorService> logger)
    {
        _eventLog = eventLog;
        _logger = logger;
    }
}
```

When NOT using primary constructors (e.g. you need field initialization logic), prefix private fields with `_`.

---

## var

Always use `var` when declaring local variables — the right side already tells you the type.

```csharp
// ✅
var authorId = AuthorId.New();
var authors = collection.Find(_ => true).ToList();

// ❌
AuthorId authorId = AuthorId.New();
List<Author> authors = collection.Find(_ => true).ToList();
```

---

## Expression-bodied members

Use for simple methods and properties:

```csharp
public string FullName => $"{FirstName} {LastName}";
public void Log(string message) => _logger.LogInformation(message);
public AuthorId Id => _id;
```

---

## Collections

```csharp
// ✅ Return IEnumerable for read-only sequences
public IEnumerable<Author> All() => _collection.Find(_ => true).ToList();

// ❌ Never expose mutable collection types from public APIs
public List<Author> All() => ...
public Dictionary<AuthorId, Author> ByName() => ...

// ✅ IReadOnlyDictionary for key-value returns
public IReadOnlyDictionary<AuthorId, Author> ByName() => ...
```

---

## Nullable Reference Types

```csharp
// ✅ is null / is not null
if (author is null) throw new AuthorNotFound();
if (name is not null) DoSomething(name);

// ❌ == null / != null
if (author == null) ...

// Trust the type system — don't add defensive null checks when annotations guarantee non-null:
// If param is not nullable, don't guard it
public void Register(AuthorName name)    // name is guaranteed non-null — no null check needed
{
    // ...
}

// Add ! when you're certain but the compiler isn't:
var author = collection.Find(a => a.Id == id).FirstOrDefault()!;
```

---

## Async

```csharp
// ✅ Async with proper naming (no Async suffix unless needed for overload disambiguation)
public async Task Append(AuthorId id, AuthorRegistered @event) =>
    await _eventLog.Append(id, @event);

// Use Task<T> for async results
public async Task<Author?> FindById(AuthorId id) => ...

// Use await — never .Result or .Wait()
var result = await _eventLog.Append(id, @event);
```

---

## Immutability

Prefer immutable designs. Use `with` expressions to create modified copies of records:

```csharp
var updated = existing with { Name = newName };
```

Avoid returning mutable objects that callers could mutate. The owner of state is responsible for mutations.

---

## Pattern matching

Use pattern matching and switch expressions wherever possible:

```csharp
// ✅ Pattern matching
if (result is Result<Author, Error>.Success success)
    return success.Value;

// ✅ Switch expression
var description = status switch
{
    Status.Active => "Active",
    Status.Inactive => "Inactive",
    _ => "Unknown"
};
```

---

## String interpolation

```csharp
// ✅
var message = $"Author '{name}' already exists";

// ❌
var message = string.Format("Author '{0}' already exists", name);
var message = "Author '" + name + "' already exists";
```

---

## Interface bodies

For interfaces with no members, omit the body:

```csharp
// ✅
public interface IMyMarker;

// ❌
public interface IMyMarker { }
```
