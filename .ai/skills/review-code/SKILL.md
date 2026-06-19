---
name: review-code
description: Use this skill when asked to review, check, or validate code in a Cratis-based project. Produces a structured review report with blocking issues and suggestions, checked against all project architecture and style standards.
---

Review changed code against all Cratis project standards and produce a structured report.

## C# Architecture

- [ ] Each slice is its own folder `<Module>/<Feature>/<Slice>/<Slice>.cs` with all backend artifacts — **no top-level `Features/` wrapper**
- [ ] Commands: `record` type with `Handle()` directly on them — no separate handler classes
- [ ] Business rejection returns a `ValidationResult` / `Result<TEvent, ValidationResult>` (or validator) — **never thrown from `Provide()`/`Handle()`** (a throw is HTTP 500, not a validation error)
- [ ] Fetched/computed handler data is in `Provide()`, not inline in `Handle()`
- [ ] Events: `record` type, no mutable/nullable properties, past tense, never carry the event-source id
- [ ] Identity concepts derive from `EventSourceId<T>` (not `ConceptAs<Guid>`)
- [ ] Projections: AutoMap is on by default — `.AutoMap()` only after `.NoAutoMap()`; projections consume events, never read models
- [ ] Model-bound query custom paths use `[Path("...")]`, not `[Route]`
- [ ] No service locator (`IServiceProvider` not injected); discover implementations with `IInstancesOf<T>`, not `IEnumerable<T>`
- [ ] Namespace matches folder under the app source root (`<RootNamespace>.<Module>.<Feature>.<Slice>`)

## C# Code Style

- [ ] File-scoped namespaces; `using` directives alphabetically sorted
- [ ] No unused `using` directives
- [ ] `is null` / `is not null` — never `== null` / `!= null`
- [ ] `var` preferred over explicit types
- [ ] No postfixes: `Async`, `Impl`, `Service` on class names
- [ ] No regions
- [ ] All public types, methods, and properties have multiline XML doc comments
- [ ] `<summary>` tags always multiline — never `/// <summary>Text</summary>` on one line
- [ ] Methods with parameters include `<param name="...">` for each
- [ ] Non-void methods include `<returns>`
- [ ] Custom exception types only — never `InvalidOperationException`, `ArgumentException`, etc.
- [ ] Exception XML docs start with "The exception that is thrown when …"
- [ ] Copyright header on every file
- [ ] Strongly-typed Concepts for all domain IDs/values (no raw `Guid`/`string` in domain models)

## TypeScript Code Style

- [ ] `const` over `let` over `var`
- [ ] Full descriptive names — never `e`, `idx`, `prev`, `dir`, `pos`
- [ ] No `any` type — `unknown` with type guards
- [ ] No `(x as any)` — use `value as unknown as TargetType`
- [ ] No unused imports
- [ ] Copyright header on every file

## Component Rules

- [ ] `CommandDialog` from `@cratis/components/CommandDialog` for command dialogs
- [ ] `Dialog` from `@cratis/components/Dialogs` for data-only dialogs
- [ ] Never imports `Dialog` from `primereact/dialog` directly
- [ ] No hard-coded hex/rgb colors — PrimeReact CSS variables only
- [ ] README.md present for complex component folders with multiple sub-components

## Performance

Performance is part of code review, not a separate pass. Flag the common degradations:

- [ ] Projections don't join on a read model; reactors don't re-query the event log inside a handler — use event data directly
- [ ] New projections can replay all historical events without crashing; events carry no large blobs
- [ ] MongoDB queries filter on indexed fields; lists that can grow return `IQueryable<T>` for server-side paging (never load all rows or hydrate the full collection for a count)
- [ ] No N+1 query pattern; response payloads include only fields the client uses
- [ ] React: large/growing lists page rather than render all rows; no inline object/array literals as props that change identity every render; `useEffect` deps are correct
- [ ] .NET: filter before materializing (no `.ToList()` before `.Where()`); don't enumerate an `IEnumerable<T>` multiple times

Classify findings: 🔴 measurable degradation at moderate load (fix before merge) · 🟡 degrades under load/scale · 🟢 minor.

## Output format

Start with: **Review result: ✅ Approved / ⚠️ Approved with comments / ❌ Changes requested**

Then list issues:
```
### <file path>

**[BLOCKING]** Line N: `problematic code`
Because: explanation
Fix:
```corrected code```
```

End with a concise summary of what passed and what must change.

---

For the full expanded checklists across all categories, see [references/CHECKLISTS.md](references/CHECKLISTS.md).
