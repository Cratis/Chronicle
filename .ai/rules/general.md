# Cratis — Project Instructions

Cratis repositories come in **two profiles**, and the rules are scoped to them. **Identify your profile first** — it decides which rules apply.

- **Application profile (default)** — you are *building an application on Cratis*: event-sourced CQRS with **Cratis Chronicle** + **Cratis Arc**, vertical slices, read models persisted to MongoDB/EF Core, and a React + Cratis Components (PrimeReact) frontend in MVVM. Most of this corpus targets this profile.
- **Framework profile** — you are *contributing to a Cratis framework repository itself* (Arc, Chronicle, Fundamentals, Components, …). These are **libraries** — source generators, the Chronicle kernel (Orleans grains + storage), client SDKs, a React component library — **not** vertical-slice event-sourced apps. The application-architecture rules here **do not apply**; follow **[framework.md](./framework.md)**.

**How to tell:** if the repo's own package is `Cratis.*` / `@cratis/*` and it *builds* the framework, you are in the framework profile. If it *consumes* Cratis to build a product, you are in the application profile.

Profile-specific rules declare a **`profile:`** in their frontmatter (`application` or `framework`); a rule **without** one is **universal** and applies everywhere — C#/TypeScript style, code quality, specs (`Cratis.Specifications`), documentation, commits/PRs, American English. In this file, everything from **Project Layout** through the **Implementation Workflow** is *application profile* (skip to the Framework profile section if you're contributing to the framework); Philosophy, Authority, Verification, Quality Gates, and the closing sections are universal.

> **Arc is a standalone CQRS framework — not bound to event sourcing.** Even within the application profile, Arc provides model-bound commands/queries, validation, authorization, and full-stack proxy generation, and works **without** Chronicle (Arc.Core does not depend on Chronicle). A `[Command]` `Handle()` does not *have* to append events — it can return a response, return `void`, or work through injected services. The event-sourcing behavior (a returned event gets appended; `EventForEventSourceId`; "never inject `IEventLog`") comes from the **Arc + Chronicle** integration. This application is event-sourced, so the slice guidance assumes event-sourced commands — read the event-centric rules as the *house default for this app*, not universal Arc laws.

The framework is convention-over-configuration. **Idiomatic Cratis is the goal — not custom abstractions over it.** When something is unclear, prefer the Cratis convention; do not invent. The rules and skills under `.ai/` are the authoritative answer — if your question is not answered there, ask rather than inferring framework behavior from package internals.

## Project Philosophy

Every rule here serves **ease of use**, **productivity**, and **maintainability**:

- **Lovable APIs** — APIs should be pleasant to use: sane defaults, flexible, extensible, overridable. If an API feels awkward, it is wrong.
- **Easy to do things right, hard to do things wrong** — convention over configuration; artifact discovery by naming/attributes; minimal boilerplate. The framework guides you into the pit of success.
- **Events are facts** — immutable records of what happened. Past tense, one purpose, never ambiguous. If you reach for a nullable property on an event, you need a second event.
- **Strongly-typed primitives are the foundation — get them right first.** The most important, load-bearing decisions in every slice are the domain primitives: `ConceptAs<T>` value types and `EventSourceId<T>` identities (never raw `Guid`/`string`/`int` for a domain value); `ConceptValidator<T>` for invariants that travel with a value everywhere it appears; `CommandValidator<T>` for command-level rules; and past-tense, self-describing `[EventType]` names. These are not boilerplate or an afterthought — they are what makes the model type-safe end to end, the rules enforceable in one place, the events trustworthy forever, and the generated proxies meaningful. Treat naming and typing them precisely as the highest-value craftsmanship in the codebase; a slice built on sloppy primitives is wrong no matter how good the rest is.
- **High cohesion through vertical slices** — everything for a behavior lives together: backend, frontend, specs. Navigate by feature, not by technical layer.
- **Full-stack type safety** — shared models flow from C# through proxy generation to TypeScript. End-to-end typing without manual synchronization.
- **Specialization over reuse** — focused, purpose-built read models over one model reused across conflicting scenarios.
- **Consistency is king** — when in doubt, follow the established pattern.

When these instructions don't cover a situation, apply these values to make the call.

## Three Levels of Authority

Every rule below is one of three kinds — know which, because they carry different weight:

- **Framework contract** — enforced by Arc/Chronicle source, analyzers, or runtime. Violating it breaks the build or behaves wrongly. (e.g. `[Command]` needs a public instance `Handle()`; model-bound queries are static methods on `[ReadModel]`; nullable event properties raise a Chronicle analyzer warning.)
- **Cratis Application convention** — the house default for maintainability and consistent generated code. The framework does **not** enforce it, but follow it for consistency. (e.g. the slice folder shape, one backend file per small slice, declaration order.)
- **Product policy** — belongs in a downstream app's own `.ai/`, not this generic corpus. (e.g. specific roles, locales, design systems.)

Where a rule is convention rather than contract, this file says so. Do not claim "the framework requires this" for a convention.

## Project-Specific Instructions

This corpus is the shared, generic instruction set common to every Cratis repository. Individual projects need extra context that does not belong here — credentials, HTTP headers, environment endpoints, and other local conventions ("Product policy" above).

- Always look for a `.agents/PROJECT.md` file at the repository root. If it exists, read it and treat its contents as additional, project-specific instructions.
- `.agents/PROJECT.md` lives in the **consuming project** and is never part of this shared corpus — it is the designated home for anything project-local, such as the HTTP headers or credentials needed to talk to that project's APIs.
- When its guidance conflicts with these shared instructions, the project-specific file wins for that repository.

## Collaboration Default

Default to agentic behavior: inspect local rules, skills, code, tests, and generated patterns; make conservative assumptions supported by that context; implement and verify end to end when feasible. Don't interrupt with questions the repository can answer. Ask when the answer can't be found locally, when reasonable product/domain choices differ meaningfully, when a change is risky, or when the user asked for checkpoints.

## Verification Discipline

A claim is only as good as the signal behind it — a build result, a test run, a lint pass, observed app behavior — not the model's own confidence. Internal reasoning *plans* the work; external signals *confirm* it.

- **Confirm "done"/"fixed"/"correct" against a fresh signal — never self-assessment.** Run the relevant gate and observe it pass *this time*.
- **After a fix, re-run the gate that failed.** Don't argue yourself to green.
- **A green build is not behavioral correctness.** Compilation proves it builds, not that the slice does the right thing — that's what specs and exercising the UI are for.
- **Report with inspectable evidence, and name what you didn't verify.**

---

# Application profile

> The following — **Project Layout, Slice Types, Slice Naming, the Rules, and the Implementation Workflow** — applies when **building an application on Cratis**. If you are contributing to a Cratis framework repo, skip to **Framework profile** below and follow [framework.md](./framework.md).

## Project Layout (Cratis Application convention)

The framework discovers commands and read models by attributes and static methods — **the folder shape is a convention, not a requirement.** The house default keeps everything for one behavior together, with **no top-level `Features/` wrapper**: the domain hierarchy lives directly under the app source root.

```
<AppSourceRoot>/                 e.g. Source, Source/Core (app-defined)
├── Common/                      shared ConceptAs<T> / EventSourceId<T> types
├── Identity/ Components/ ...    cross-cutting / shared concerns, at the top level
└── <Module>/                    top-level domain area — natural for most apps, NOT required
    └── <Feature>/               grouping within the domain area
        ├── <Feature>.tsx        pass-through layout (renders <Outlet/>)
        ├── <Concept>.cs         feature-level concept types
        └── <Slice>/             one folder per behavior — the invariant unit
            ├── <Slice>.cs       backend artifacts for the slice in one file
            ├── *.tsx            React component(s) for the slice
            └── when_*/          spec folders
```

**The slice is the invariant unit** — one behavior (command + events + projection + component + specs), created/renamed/deleted together. Features group related slices. A `<Module>` is the natural top-level domain grouping for larger areas (e.g. `Accounts`, `Admin`, `Requests`) but is **not required** — a feature may sit directly under the source root when no module grouping is natural; depth follows what fits the application. Cross-cutting concerns (shared concepts in `Common/`, shared components, identity) live at the top level. Namespace mirrors the path under `<AppSourceRoot>` (`<RootNamespace>.<Module>.<Feature>.<Slice>`, dropping any level that isn't present). Splitting the backend file is allowed when a slice grows large or shared concepts move upward; the single-file shape is the default, not a mandate.

> **No top-level `Features/` wrapper** — modules/features live directly under the app source root. The framework enforces no layout; this nested domain hierarchy is the chosen Cratis Application convention.

## Slice Types

Pick exactly one type per slice folder — determined by what the slice *does*.

| Type | What it does | Contents |
|---|---|---|
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
3. **[contract] Event-source id resolution order:** `ICanProvideEventSourceId` → an `EventSourceId`/`EventSourceId<T>`-derived value → `[Key]` → else Arc/Chronicle generates one. Identity concepts derive from `EventSourceId<T>` with the underlying primitive (define `NotSet`, a typed `New()`, and a primitive→id operator) — never `ConceptAs<Guid>` for an event-source identity.
4. **[contract] Events never carry the event-source id** — it is implicit in the event context.
5. **[contract] `[Key]` / `[Subject]` are distinct.** `[Key]` is for event-source/read-model/projection key resolution; `[Subject]` is compliance identity only. Don't put either on an `EventSourceId<T>` value (it already is both); use them only for non-`EventSourceId<T>` values.
6. **[contract] Avoid nullable event properties** — Chronicle's analyzer warns on them. Model optional facts as a separate event; resolve nullable command inputs to a non-null sentinel before constructing the event.
7. **[contract] `[EventType]` takes no arguments for new events** — the type name is the identifier. Use `generation:`/id only when evolving an existing contract; schema changes get a new generation + an `EventTypeMigration<TUpgrade,TPrevious>` (never edit stored-event semantics silently).
8. **[convention] Every `[EventType]` has an XML `<summary>`** — a Cratis C# documentation convention (not a Chronicle rule); events live in the log forever, so record why they exist.
9. **[convention] `[ReadModel]` properties carry no default values** except semantically meaningful enum initial states and `[SetValue<T>]`-driven `bool` flags. False defaults hide missing projection wiring.
10. **[contract] AutoMap is on by default — never call `.AutoMap()`.** Match property names so AutoMap wires them; diverge with `[SetFrom<T>]` / fluent `.Set().To()` only for genuine name differences. Re-enable `.AutoMap()` only inside a scope where it was disabled with `.NoAutoMap()`.
11. **[contract] Projections consume events and event context — never other read models.** Default to model-bound attributes; use fluent `IProjectionFor<T>` for joins/nested/context/transforms; use a reducer when the model is "current state + event → next state" (a valid style, not a failure mode).
12. **[contract] Model-bound query custom paths use `[Path("...")]`** (`PathAttribute`), not ASP.NET `[Route]`. Reserve `[Route]` for controller-based endpoints (which this convention avoids).
13. **[convention] Cross-slice access is read-only through Chronicle** — inject another slice's read model or reference its events; never instantiate or DI another slice's command/handler/service.
14. **[contract] Never inject `IEventLog` into `Handle()`** — express appends through return types (`IEnumerable<object>` with `EventForEventSourceId` wrappers for cross-stream). In application reactors, return side-effect events or use `ICommandPipeline`; don't reach for `IEventLog` directly.
15. **[contract] Never edit a generated file** — proxies carry a `// @generated by Cratis` header. Fix the C# source and rebuild.
16. **[convention] Use the Cratis dialog wrappers** — never import `Dialog` from `primereact/dialog`; use `CommandDialog` / `Dialog` from `@cratis/components`. The default frontend stack is Cratis Components on PrimeReact theming/tokens/`pt` — **not** Tailwind (Tailwind is one supported unstyled path, not the generic default).
17. **[convention] One slice is one unit** — creating/renaming/moving/deleting a slice means doing the same to every artifact (the `.cs`, every `when_*/`, every `.tsx`, the composition import/JSX, the route).

## Implementation Workflow

- **Phase 0 — Model the request.** Confirm Module/Feature, Slice name, slice type, domain rules. For new behavior or unclear event vocabulary, run the **event-modeling** skill before writing code.
- **Phase 1 — Backend.** Write the slice file. **Gate:** build clean Debug *and* Release (Release regenerates the TypeScript proxies; Debug compiles `#if DEBUG` spec code).
- **Phase 2 — Specs.** Mandatory for every slice type, in-process scenario family first: `CommandScenario<T>` (commands), `EventScenario` (constraints/append), `ReadModelScenario<T>` (projections/reducers), `ReactorScenario<T>` (reactors). Reserve out-of-process integration specs for host/infra/transport boundaries. **Gate:** tests pass.
- **Phase 3 — Frontend.** Proxies now exist. Build React components from generated proxies, register in the composition page, wire routing. **Gate:** lint, conditional test, and build all clean.

**Backend before frontend, always** — the frontend depends on proxies that only exist after a successful Release build. After creating each new file, build (C#) or compile (TypeScript) before moving on — fix every error as it appears rather than accumulating it.

## Quality Gates

| Phase | Command (app-pinned) | Pass criteria |
|---|---|---|
| Backend | build (Debug) | zero errors, zero warnings — validates `#if DEBUG` spec code |
| Backend | build (Release) | zero errors, zero warnings — regenerates proxies |
| Specs | test | zero failures |
| Frontend | lint | zero errors |
| Frontend | test | zero failures when frontend specs/behavior changed |
| Frontend | build | zero errors |

All gates pass before merging, opening a PR, or marking a slice complete. After pushing to a PR, monitor CI with the GitHub MCP tools (`pull_request_read` → `get_check_runs`, `get_job_logs`); investigate and fix any failure, then push again — the task is not done until CI is green or the only remaining failures are confirmed pre-existing flakes unrelated to the change.

---

# Framework profile

> You are contributing to a Cratis framework repository (Arc, Chronicle, Fundamentals, Components, …). **The Application-profile sections above do not apply** — there are no vertical slices, model-bound `[Command]`/`[ReadModel]` artifacts, projections/read-models, or MVVM app components here; these are libraries. Follow **[framework.md](./framework.md)** for repo structure, library/API design, source generators, the Chronicle kernel, and the framework quality gates. The universal sections (below, and every `profile: universal` rule) still apply.

---

# Both profiles (universal)

## Definition of Done

- The affected solution/project builds with zero warnings and zero errors.
- Relevant specs for every affected project pass.
- For public-facing changes (clients, SDKs, public APIs, developer-facing behavior), documentation is added or updated and its verification passes.

## Where to Look

| For | Location |
|---|---|
| **Contributing to a Cratis framework repo** (framework profile) | `framework.md` |
| Slice anatomy (commands, `Provide()`, validators, events, projections, read models, reactors, constraints, compliance, cross-slice) | `vertical-slices.md` |
| C# / TypeScript style | `csharp.md`, `typescript.md` |
| React + Arc + Cratis Components + MVVM + dialogs | `react.md`, `components.md`, `dialogs.md` |
| Frontend engineering quality & testing | `frontend-quality.md`, `frontend-testing.md`, `storybook.md` |
| Spec patterns — universal `Specification` base (both profiles) | `specs.md`, `specs.csharp.md`, `specs.typescript.md` |
| Spec patterns — the four `*Scenario` helpers (application only) | `specs.scenarios.csharp.md` |
| Strongly-typed values (`ConceptAs<T>`, `EventSourceId<T>`) | `concepts.md` |
| Shared term definitions (event, projection, reducer, reactor, observer, DCB, …) | `glossary.md` |
| Diagnosing a misbehaving slice (read model stale, proxy missing, quarantine, …) | the **diagnose-slice** skill |
| EF Core read models / migrations | `efcore.md`, `efcore.specs.md` |
| PRs / commits | `pull-requests.md`, `git-commits.md` |
| Event modeling / schema migration / calling commands from code / paging / cross-cutting metadata / multi-tenancy | the matching skills |
| Step-by-step recipes | `.ai/skills/` |

## Source-of-Truth Discipline

- **Rules define invariants; skills define workflows.** A skill may refine how to apply a rule but must not contradict it. On conflict, follow the stricter invariant and fix the stale artifact.
- **Skills and rules are the authoritative answer.** If not answered there, ask. Don't infer Cratis behavior from package internals.
- Only make high-confidence suggestions.
- Don't change dependency manifests / lockfiles / `global.json` / NuGet config unless explicitly asked.
- When asked to commit, push, create a PR, ship, or land changes, use the **ship-changes** skill.

## General

- **American English** in all code, comments, and docs (initialize, behavior, color, serialize…).
- Treat warnings as errors; never suppress warning output.
- Reuse the active terminal for commands; create a new one only when the current one is busy or fails.
- All files start with the standard license header:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```
