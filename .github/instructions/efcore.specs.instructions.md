---
applyTo: "**/for_*/**/*.cs, **/when_*/**/*.cs"
---

# Entity Framework Core Specs

EF Core specs verify database interaction logic — migrations, queries, and error handling. They use SQLite in-memory databases, which are fast, isolated, and disposable. This keeps specs independent of any real database server.

These specs are for code that interacts directly with `DbContext`. For event-sourcing integration specs (testing commands against Chronicle), use the Chronicle integration spec pattern in [specs.csharp.instructions.md](./specs.csharp.instructions.md) instead.

## Database Setup

- Use SQLite in-memory database for all `DbContext` specs — it's fast and each spec gets a clean database.
- Configure and dispose the in-memory database properly to avoid state leakage between tests. Each spec run should start with a fresh schema.

## Mocking DbContext

- `SaveChanges()` and `SaveChangesAsync()` are virtual — mock with NSubstitute.
- `DbSet<T>` methods are virtual — mock as needed.
- Pass options when substituting: `Substitute.For<YourDbContext>(options)`.

```csharp
var options = new DbContextOptionsBuilder<YourDbContext>()
    .UseSqlite("DataSource=:memory:")
    .Options;

var context = Substitute.For<YourDbContext>(options);
```

## Simulating Failures

Mock `SaveChanges` / `SaveChangesAsync` to throw exceptions for testing error handling and recovery:

```csharp
context.SaveChangesAsync(Arg.Any<CancellationToken>())
    .Throws(new DbUpdateException("Simulated failure"));
```
