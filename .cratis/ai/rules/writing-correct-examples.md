---
applyTo: "**/Documentation/**/*.{md,mdx}"
paths:
  - "**/Documentation/**/*.md"
  - "**/Documentation/**/*.mdx"
---
<!-- cratis-ai-managed: rules/writing-correct-examples.md -->

# Writing Correct Code Examples (Technical Docs)

Documentation code examples are **copied verbatim** by evaluators. A snippet that uses an API that doesn't exist is worse than no snippet — it breaks on first paste and loses trust. An audit of these docs found **~12 fabricated-API bugs** that had passed review and shipped. The discipline below is how you avoid adding the thirteenth.

## The rule: verify every framework API against real source — before you write it

For each framework type, attribute, method, prop, hook, or import in an example, confirm it exists and has that exact shape in **source**, not in another doc page (the docs themselves had the bugs):

- **C# / backend** — grep real usage in a reference application (e.g. Cratis **Studio**) and the product `Source/` trees of the Cratis repos checked out alongside this one (`Arc/Source`, `Chronicle/Source`). For extension methods, find the `public static … (this <Type> …)` signature and note **which type it extends**.
- **React / Components** — the authoritative prop names are in the compiled type defs of the installed package (`node_modules/@cratis/components/dist/esm/**/*.d.ts`) or the `Components` source `dist`. Real usage: a reference app's `*.tsx`.
- **Invented *domain* names are fine** (event/concept/command names like `AuthorRegistered`, `BookId`). Only **framework APIs** must be real. Never invent a framework interface, attribute, prop, method, or import path.
- **Read the source at the version the reader runs, never at a checkout's `HEAD`.** A sibling clone's working tree is whatever someone last checked out; the package the reader installed is a tag. Read with `git show <tag>:<path>` and `git grep <pattern> <tag> -- <path>` — both work without touching the checkout, so a dirty or shared worktree is never a reason to skip the check. Name the repository and the tag in the claim ("`Arc v22.16.0`, `ParameterDependencyResolver.cs:44-62`") so the next reader can re-run it. Where the corpus states a version in a skill's *Verified product sources* table, that tag is the one to read; when a consumer's pin is newer, re-read at theirs. A release note is a reason to look, not evidence that a behavior changed or that a workaround can be retired — retire a workaround only after the original reproduction passes at the new tag.

## Choose a maintained source for the example

For a long example or threaded tutorial, derive displayed snippets from a
compiling, tested sample or spec when tooling supports extraction; otherwise
compare each block to that source and check both together. For a short,
illustrative excerpt, write purpose-built code but verify its framework APIs
against the product source at the supported version. Do not forbid copying
from a runnable sample: copying *without a check that prevents drift* is the
problem. Multi-client pages use their client-owned snippet sources and checks;
never hand-translate an unsupported SDK. See the **cratis-technical-examples**
skill for the workflow.

## Complete and correct

- No pseudo-code, no `// ...` elisions that leave the reader guessing, no props/members that don't exist.
- A standalone snippet should compile with its stated prerequisites. Label an excerpt as an excerpt and supply or link the domain types it assumes.
- Show the run command and an observable result for a substantial sample; a passing site build does not prove behavior.

## Historical API pitfalls to recheck

These reminders are not versioned API evidence and must not be copied as an
unchecked contract. Before using one in a new example, resolve the reader's
package version and verify the exact receiver, signature and source path at
that tag. Recheck affected examples on product version changes; remove a
workaround only after its reproduction passes. The source check above, not
this list, establishes which shape the target version supports.

- Commands/queries are **model-bound**: a `[Command]` record with `Handle()` **on the record**, and `[ReadModel]` records with **static** query methods. The marker/handler interfaces `ICommand`, `ICommandHandler<T>`, `IQuery<T>`, `IQueryHandler<T,R>` **do not exist** — never use them.
- Bootstrap: `ArcApplication.CreateBuilder(args)` (not `ArcApplicationBuilder.CreateBuilder`). `builder.AddCratisArc()` on the builder (`WebApplicationBuilder`/`IHostBuilder`); `app.UseCratisArc()` on the built app and it takes **no args** (the listen URL comes from `ArcOptions.Hosting.ApplicationUrl`).
- Read the current user inside `Handle()` by injecting **`IHttpContextAccessor`** and reading `HttpContext?.User` (`ClaimsPrincipal`). There is no `CommandContext.User` and no `IUserAccessor` Arc type. In-`Handle` guards return **`Result<TEvent, ValidationResult>`** (success type first, error type second) + `ValidationResult.Error(...)` — there is no `CommandResult.Forbidden`/`Unauthorized` to return.
- Components: `DataPage` uses the **compound** `DataPage.Columns` / `DataPage.MenuItems`; the detail prop is **`detailsComponent`** (lowercase) — `detailsTitle`/`initialSizes` are **not** props. Import `DataTableForObservableQuery` from `@cratis/components/DataTables` (the root barrel only re-exports namespaces); `DataPage`/`MenuItem` from `@cratis/components/DataPage`. Required props like `emptyMessage`/`title` must be present.
- Chronicle model-bound projections use property attributes **`[SetFrom<T>]`** / **`[SetValue<T>]`** (and `[FromEvent<T>]` AutoMap) — **not** `static On(event)` methods (that shape does not exist). Retrieve a read model with `eventStore.ReadModels.GetInstanceById<T>(id)`. Assertion signatures depend on the extension receiver: the out-of-process `IChronicleSetupFixture` extension is `ShouldHaveAppendedEvent<TEvent>(sequenceNumber, eventSourceId, validator)`, while Arc's in-process `CommandScenario<TCommand>` extensions use `<TCommand, TEvent>` with the event-source id and optional predicate. Verify the receiver type and its exact extension signature before copying either shape.

## Auditing at scale

Re-run a snippet-correctness audit periodically — it keeps finding bugs (the list above came from three rounds). Delegate the cross-checking to subagents that compare each snippet to source and report only confirmed discrepancies; **verify each finding against source yourself before fixing**. Consider adopting an automated example tester (**Doc Detective**, **Squidler** — from awesome-docs) that actually runs the snippets, so correctness is enforced by CI rather than by hand. The principle, from jvns's "write good examples by starting with real code": derive examples from working source, don't compose them from memory.
