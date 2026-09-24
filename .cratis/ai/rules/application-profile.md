---
applyTo: "**/*"
profile: application
---
<!-- cratis-ai-managed: rules/application-profile.md -->

# Application profile

> The following — **Project Layout, Slice Types, Slice Naming, the Rules, and the Implementation Workflow** — applies when **building an application on Cratis**. If you are contributing to a Cratis framework repo, skip to **Framework profile** below and follow [framework.md](./framework.md).

## Project Layout (Cratis Application convention)

The framework discovers commands and read models by attributes and static methods — **the folder shape is a convention, not a requirement.** The house default: the domain hierarchy lives directly under the app source root (**no top-level `Features/` wrapper**), cross-cutting concerns (`Common/` concepts, `Identity/`, shared components) at the top level, and one folder per behavior — `<Module>/<Feature>/<Slice>/` holding `<Slice>.cs`, the slice's `.tsx`, and its `when_*/` specs. **The slice is the invariant unit** — created, renamed, and deleted as one. Namespace mirrors the path under the source root, dropping any level that isn't present. The full tree and the split rules are in [vertical-slices.md](./vertical-slices.md).

## Slice Types

Pick exactly one type per slice folder — determined by what the slice *does*.

| Type | What it does | Contents |
| --- | --- | --- |
| **State Change** | Accepts a command, appends events | Command + validator + event(s); optional `[Passive]` read model for command-side decisions |
| **State View** | Projects events into a queryable read model | `[ReadModel]` + model-bound projection + static query method(s) |
| **Automation** | Reacts to events, calls external systems / `ICommandPipeline` | Reactor only |
| **Translation** | Reacts to events and appends follow-up events to another stream | Reactor only |

## Slice Naming (convention)

Commands are imperative intents (`Register`, `Create`); the slice folder is the action only, never repeating a noun the Feature already establishes. **`[EventType]` records are past-tense facts** and must be self-describing (`AuthorRegistered`, never `Created`) — this past-tense, one-purpose naming is a Chronicle framework recommendation. Static query methods are descriptive reads (`AllAuthors`, `AuthorById`, `AuthorsByName`).

## Rules

Tagged **[contract]** (framework-enforced) or **[convention]** (house default). Mechanics and examples live in `vertical-slices.md`.

1. **[contract] Model-bound — no controllers.** Commands are `[Command]` records with a public instance `Handle()` (Arc analyzers enforce this); queries are `static` methods on `[ReadModel]` records; projections/constraints/authorization use attributes. Arc generates the HTTP surface. Drop to fluent (`IProjectionFor<T>`, `IConstraint`) only when model-bound can't express the rule.
2. **[contract] Command validation & data flow.** Put command rejection in `CommandValidator<T>`, global value invariants in `ConceptValidator<T>`, and fetched/computed handler data in **`Provide()`** (runs after validation/authorization; may short-circuit with `ValidationResult.Error` / `Result<TProvided, ValidationResult>`). Keep `Handle()` focused on event construction. For a state-dependent rule that must hold **under concurrency**, inject the read model into `Handle()` and return a typed error via `Result<TEvent, ValidationResult>`. **Throwing from `Provide()`/`Handle()` is an exception (HTTP 500), not a validation rejection** — throw only for genuinely exceptional conditions, never for normal business rejection.
3. **[contract] Event-source id resolution:** `ICanProvideEventSourceId` first; else the first qualifying property in declaration order — an `EventSourceId`/`EventSourceId<T>`-derived value *or* a `[Key]` property (no priority between them; two candidates is `ARCCHR0002`); else Arc generates one. A value actually used as a Chronicle stream identity derives from `EventSourceId<T>` with the underlying `IComparable` primitive — never `ConceptAs<Guid>` for that stream identity. `NotSet`, `New()`, and primitive→derived-id operators are optional domain/API conveniences, not Chronicle requirements; typed empty/zero values are real specified stream IDs, not `EventSourceId.Unspecified`.
4. **[contract] Events never carry the event-source id** — it is implicit in the event context.
5. **[contract] `[Key]` / `[Subject]` are distinct.** `[Key]` is for event-source/read-model/projection key resolution; `[Subject]` is compliance identity only. Don't put either on an `EventSourceId<T>` value (it already is both); use them only for non-`EventSourceId<T>` values.
6. **[contract] Avoid nullable event properties** — Chronicle's analyzer warns on them. Model optional facts as a separate event; resolve nullable command inputs to a non-null sentinel before constructing the event.
7. **[contract] `[EventType]` takes no arguments for new events** — the type name is the identifier. Use `generation:`/id only when evolving an existing contract; schema changes get a new generation + an `EventTypeMigration<TUpgrade,TPrevious>` (never edit stored-event semantics silently). An enum that only gains a member, or has one renamed, is the exception — Chronicle accepts that in place; a *removed* or *renumbered* member still needs a generation, plus a value map saying what the old values became.
8. **[convention] Every `[EventType]` has an XML `<summary>`** — a Cratis C# documentation convention (not a Chronicle rule); events live in the log forever, so record why they exist.
9. **[convention] `[ReadModel]` properties carry no default values** except semantically meaningful enum initial states and `[SetValue<T>]`-driven `bool` flags. False defaults hide missing projection wiring.
10. **[contract] AutoMap is on by default — never call `.AutoMap()`.** Match property names so AutoMap wires them; diverge with `[SetFrom<T>]` / fluent `.Set().To()` only for genuine name differences. Re-enable `.AutoMap()` only inside a scope where it was disabled with `.NoAutoMap()`.
11. **[contract] Projections consume events and event context — never other read models.** Default to model-bound attributes; use fluent `IProjectionFor<T>` for joins/nested/context/transforms; use a reducer when the model is "current state + event → next state" (a valid style, not a failure mode).
12. **[contract] Model-bound query custom paths use `[Path("...")]`** (`PathAttribute`), not ASP.NET `[Route]`. Reserve `[Route]` for controller-based endpoints (which this convention avoids).
13. **[convention] Cross-slice access is read-only through Chronicle** — inject another slice's read model or reference its events; never instantiate or DI another slice's command/handler/service.
14. **[contract] Never inject `IEventLog` into `Handle()`** — express appends through return types (`IEnumerable<object>` with `EventForEventSourceId` wrappers for cross-stream). In application reactors, return side-effect events or commands (or use `ICommandPipeline` and inspect the result); don't reach for `IEventLog` directly.
15. **[contract] Never edit a generated file** — proxies carry a `// @generated by Cratis` header. Fix the C# source and rebuild.
16. **[convention] Use the Cratis dialogs** — `CommandDialog` from `@cratis/components/CommandDialog`, `Dialog` from `@cratis/components/Dialogs`; never a vendor or hand-rolled modal. The default frontend stack is Cratis Components **4.x** (Components-owned markup, `--cratis-*` tokens, `pt`/`data-cratis-part` parts; no PrimeReact dependency) — **not** Tailwind (Tailwind is one supported way to write the token-mapping CSS, not the generic default).
17. **[convention] One slice is one unit** — creating/renaming/moving/deleting a slice means doing the same to every artifact (the `.cs`, every `when_*/`, every `.tsx`, the composition import/JSX, the route).

## Implementation Workflow

- **Phase 0 — Model.** Confirm Module/Feature, slice name, slice type, domain rules; for new behavior or unclear event vocabulary run the **event-modeling** skill first.
- **Phase 1 — Backend.** Implement a coherent slice change. **Gate:** incremental Debug build of the affected project (regenerates proxies, compiles `#if DEBUG` spec code).
- **Phase 2 — Specs.** Mandatory for every slice type, in-process scenario family first (`CommandScenario<T>`, `EventScenario`, `ReadModelScenario<T>`, `ReactorScenario<T>`). **Gate:** tests pass.
- **Phase 3 — Frontend.** Build from the generated proxies, register in the composition page, wire routing. **Gate:** lint, conditional test, build.

**Backend before frontend, always** — the frontend depends on proxies that exist only after a successful build. Generate proxies from the **Debug** build (house convention); verify Release without re-running the generator: `dotnet build -c Release -p:CratisProxiesOutputPath=`. Why that property, and what to do when nothing is generated: the **cratis-arc-command** skill's proxy-generation reference.

## Quality Gates

| Phase | Command (app-pinned) | Pass criteria |
| --- | --- | --- |
| Backend | build (Debug) | zero errors, zero warnings — validates `#if DEBUG` spec code and regenerates proxies |
| Backend | build (Release) | zero errors, zero warnings — build-only check; pass `-p:CratisProxiesOutputPath=` to skip re-running proxy generation |
| Specs | test | zero failures |
| Frontend | lint | zero errors |
| Frontend | test | zero failures when frontend specs/behavior changed |
| Frontend | build | zero errors |

Run affected-project incremental checks after a coherent change, then targeted regression tests for the changed behavior. Re-run a failed gate after a relevant fix. Reserve wider matrices and clean/Release builds for cross-cutting changes, demonstrated stale outputs, or required merge/release gates. Documentation/rule-only edits need relevant Markdown, frontmatter, link, and corpus checks, not an application build. Diagnose unrelated or environmental failures within a bounded attempt; report the evidence and blocker instead of broadening scope or retrying indefinitely. Required gates remain blocking until satisfied; never silently waive red CI.

Documentation-only changes carry non-release intent and run the documentation checks, never a blanket exemption from red CI — see [pull-requests.md](./pull-requests.md).
