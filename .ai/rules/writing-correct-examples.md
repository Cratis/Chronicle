---
applyTo: "**/Documentation/**/*.{md,mdx}"
paths:
  - "**/Documentation/**/*.md"
  - "**/Documentation/**/*.mdx"
---

# Writing Correct Code Examples (Technical Docs)

Documentation code examples are **copied verbatim** by evaluators. A snippet that uses an API that doesn't exist is worse than no snippet — it breaks on first paste and loses trust. An audit of these docs found **~12 fabricated-API bugs** that had passed review and shipped. The discipline below is how you avoid adding the thirteenth.

## The rule: verify every framework API against real source — before you write it

For each framework type, attribute, method, prop, hook, or import in an example, confirm it exists and has that exact shape in **source**, not in another doc page (the docs themselves had the bugs):

- **C# / backend** — grep real usage in **Studio** (`/Volumes/sourcecode/repos/cratis/Studio/Source`, `*.cs`) and the product `Source/` trees (`Arc/Source`, `Chronicle/Source`). For extension methods, find the `public static … (this <Type> …)` signature and note **which type it extends**.
- **React / Components** — the authoritative prop names are in the compiled type defs: `Components/Source/dist/esm/**/*.d.ts`. Real usage: Studio `*.tsx`.
- **Invented *domain* names are fine** (event/concept/command names like `AuthorRegistered`, `BookId`). Only **framework APIs** must be real. Never invent a framework interface, attribute, prop, method, or import path.

## Complete and correct

- No pseudo-code, no `// ...` elisions that leave the reader guessing, no props/members that don't exist.
- A snippet a reader pastes should compile (modulo the invented domain types they'd supply).

## Verified gotchas (the real APIs — these are the ones docs kept getting wrong)

- Commands/queries are **model-bound**: a `[Command]` record with `Handle()` **on the record**, and `[ReadModel]` records with **static** query methods. The marker/handler interfaces `ICommand`, `ICommandHandler<T>`, `IQuery<T>`, `IQueryHandler<T,R>` **do not exist** — never use them.
- Bootstrap: `ArcApplication.CreateBuilder(args)` (not `ArcApplicationBuilder.CreateBuilder`). `builder.AddCratisArc()` on the builder (`WebApplicationBuilder`/`IHostBuilder`); `app.UseCratisArc()` on the built app and it takes **no args** (the listen URL comes from `ArcOptions.Hosting.ApplicationUrl`).
- Read the current user inside `Handle()` by injecting **`IHttpContextAccessor`** and reading `HttpContext?.User` (`ClaimsPrincipal`). There is no `CommandContext.User` and no `IUserAccessor` Arc type. In-`Handle` guards return `Result<ValidationResult, T>` + `ValidationResult.Error(...)` — there is no `CommandResult.Forbidden`/`Unauthorized` to return.
- Components: `DataPage` uses the **compound** `DataPage.Columns` / `DataPage.MenuItems`; the detail prop is **`detailsComponent`** (lowercase) — `detailsTitle`/`initialSizes` are **not** props. Import `DataTableForObservableQuery` from `@cratis/components/DataTables` (the root barrel only re-exports namespaces); `DataPage`/`MenuItem` from `@cratis/components/DataPage`. Required props like `emptyMessage`/`title` must be present.
- Chronicle model-bound projections use property attributes **`[SetFrom<T>]`** / **`[SetValue<T>]`** (and `[FromEvent<T>]` AutoMap) — **not** `static On(event)` methods (that shape doesn't exist). Retrieve a read model with `eventStore.ReadModels.GetInstanceById<T>(id)`. Integration assertion is `ShouldHaveAppendedEvent<TEvent>(seq, eventSourceId, validator)` (**3 args**).

## Auditing at scale

Re-run a snippet-correctness audit periodically — it keeps finding bugs (the list above came from three rounds). Delegate the cross-checking to subagents that compare each snippet to source and report only confirmed discrepancies; **verify each finding against source yourself before fixing**. Consider adopting an automated example tester (**Doc Detective**, **Squidler** — from awesome-docs) that actually runs the snippets, so correctness is enforced by CI rather than by hand. The principle, from jvns's "write good examples by starting with real code": derive examples from working source, don't compose them from memory.
