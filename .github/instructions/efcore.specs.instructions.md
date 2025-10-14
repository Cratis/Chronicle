---
applyTo: "**/for_*/**/*.cs, **/when_*/**/*.cs"
---

# How to write Entity Framework Core Specs

- Use Sqlite in-memory database for specs (tests) that involve `DbContext` operations. This allows for lightweight and fast testing without the need for a full database setup.
- Ensure that the in-memory database is properly configured and disposed of after each test to avoid state leakage between tests.
- When writing specs (tests) for units that leverage a `DbContext` or a derivative of it, remember that methods like `SaveChanges` or `SaveChangesAsync` are virtual and can be mocked.
- Simulating failure on a `DbContext` operation can be achieved by mocking these methods to throw exceptions, allowing you to test error handling and recovery logic in your specs.
- `DbSet` has most of its methods as virtual, which means you can mock them as needed for your tests.
- A `DbContext` needs options passed into its constructor. When mocking with NSubstitute you simply pass the options when substituting it, e.g., `Substitute.For<YourDbContext>(options)`.
