---
applyTo: "**/*.cs"
---

# Code Quality — C#

C#-specific applications of the general [Code Quality](./code-quality.instructions.md) principles.

## Composition over Inheritance

Use constructor injection to compose behavior. Primary constructors make this natural in modern C# — the type's dependencies are visible at a glance and can be substituted in tests.

```csharp
// ❌ Inheritance — child is tightly coupled to parent internals
public class ReportExporter : BaseExporter
{
    public override void Export(Report report) { ... }
}

// ✅ Composition — behavior is injected and interchangeable
public class ReportExporter(IExportStrategy strategy)
{
    public void Export(Report report) => strategy.Execute(report);
}
```

**Rules:**
- Never extend a concrete class to add or change behavior — inject a collaborator instead.
- Use interfaces and `ConceptAs<T>` record wrappers rather than inheritance chains.
- Inheritance is acceptable only for framework integration points with a well-defined extension mechanism (e.g. `Specification`, `Migration`, `AggregateRoot`).

## Open/Closed Principle

The framework's `IInstancesOf<T>` mechanism makes the open/closed pattern effortless — adding a new implementation is all it takes to extend behavior. Use it instead of growing `switch` statements.

```csharp
// ❌ Modified every time a new format is added
public class ReportFormatter
{
    public string Format(Report report, string formatType)
    {
        if (formatType == "csv") return FormatAsCsv(report);
        if (formatType == "json") return FormatAsJson(report);
        throw new UnknownFormat(formatType);
    }
}

// ✅ New formats added by implementing the interface — no existing code changes
public interface IReportFormatter
{
    string Format(Report report);
}

public class CsvReportFormatter : IReportFormatter { ... }
public class JsonReportFormatter : IReportFormatter { ... }
```

**Rules:**
- Prefer strategy interfaces over `switch`/`if-else` chains that grow over time.
- Use `IInstancesOf<T>` to discover all implementations by convention — no manual registration needed.
- Design public APIs as contracts (interfaces/records) rather than concrete implementations.

## Separation of Concerns

The Chronicle + Arc stack has clear layer boundaries. Violating them creates coupling that is hard to undo.

**Rules:**
- Domain types must not reference EF Core, MongoDB, or HTTP concepts directly.
- Command handlers express intent in domain terms — they delegate persistence and I/O to injected collaborators.
- Projections build read models; they must not trigger commands or produce side effects.
- Reactors handle side effects; they must not directly read or write the event log.

## Low Coupling

**Rules:**
- Depend on abstractions (interfaces, records), not on concrete implementations.
- Use constructor injection — it makes dependencies explicit and testable.
- Avoid reaching through an object to call methods on its dependencies (`a.B.C.Do()` is a sign of tight coupling).
- Limit constructor dependencies to four or five — more is a signal the type is doing too much.
- Never reference types from unrelated features directly; go through a shared contract or event instead.

## Cross-Cutting Concerns

**Rules:**
- Never write logging statements directly inside command handlers, projections, or domain types. Use the `[LoggerMessage]` pattern in a co-located `*Logging.cs` partial class.
- Never perform authorization checks inside domain logic — express them as attributes or middleware applied at the boundary.
- Never duplicate error-handling or retry logic across handlers — centralize it in a pipeline or middleware.
- Use `ICommandPipeline`, middleware, and decorators to apply cross-cutting concerns at the infrastructure layer so that domain code remains unaware of them.
- When you notice the same infrastructural pattern appearing in two or more places (logging a specific event, catching a specific exception, checking a specific condition), extract it into a shared cross-cutting mechanism rather than duplicating it.
