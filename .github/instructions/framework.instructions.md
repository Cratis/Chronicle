---
applyTo: "**/*"
profile: framework
---

# Framework Profile — Contributing to Cratis Itself

> **Framework profile only.** This applies when you are working **inside a Cratis framework repository** (Arc, Chronicle, Fundamentals, Components, Specifications, and the like) — building the framework. If you are building an *application on* Cratis, ignore this file and follow the Application profile (`general.md` + `vertical-slices.md`).

The framework repos are **libraries**, not event-sourced applications. The application-profile architecture — vertical slices, model-bound `[Command]`/`[ReadModel]` artifacts, projections/read-models, reactors, MVVM app components — **does not exist here and must not be imposed**. A Cratis framework repo has its own architecture per its purpose.

## What still applies (universal rules)

Everything tagged `profile: universal` holds in framework repos exactly as in apps: **C# conventions** (`csharp.md`), **TypeScript conventions** (`typescript.md`), **code quality** (`code-quality*.md`), **specs** (`specs.md` / `specs.csharp.md` / `specs.typescript.md` — `Cratis.Specifications` is how the framework tests itself, using the plain `Specification` base + NSubstitute against the classes under test — this is the dominant mode in framework repos. The in-process `*Scenario` family lives in `specs.scenarios.csharp.md` (`profile: application`); it is the *application* default, and a framework repo reaches for it only to test the very engine it provides — Arc → `CommandScenario`, Chronicle → `EventScenario`/`ReadModelScenario`/`ReactorScenario` — not as a general testing mode. The **write-specs** skill is application-oriented), **documentation**, **git-commits**, **pull-requests**, **concepts** (`ConceptAs<T>` / `EventSourceId<T>` are Fundamentals/Chronicle primitives the framework defines and uses), and **American English**.

## What does NOT apply

Skip these `profile: application` rules entirely when in a framework repo: `vertical-slices.md`, `reactors.md`, `react.md`, `components.md`, `dialogs.md`, `frontend-quality.md`, `frontend-testing.md`, `storybook.md`, `efcore.md`, `efcore.specs.md`. Do not create vertical-slice folders, `[Command]`/`Handle()` records, read models, or MVVM app components in framework source.

## The repos and their shape

Each framework repo is organized by what it builds, not by feature slices:

- **Fundamentals** — the base library. `ConceptAs<T>`, type discovery (`IInstancesOf<T>` / `IImplementationsOf<T>`), serialization, common primitives. `Source/DotNET` (C#) + `Source/JavaScript` (`@cratis/fundamentals`) + the shared ESLint config.
- **Arc** — the CQRS + model-binding + proxy-generation engine. `Source/DotNET` (the command/query pipeline, validation, authorization, identity, the Roslyn **proxy generator**) + `Source/JavaScript` (`@cratis/arc`, `@cratis/arc.react`, `@cratis/arc.react.mvvm`). Arc.Core does **not** depend on Chronicle.
- **Chronicle** — the event-sourcing engine. `Source/Kernel` (the engine: **Orleans grains**, event sequences, observers/projections/reducers, storage providers — MongoDB and others), `Source/Clients` (the client SDKs incl. `DotNET` and `Testing`), `Infrastructure`, `Tools`, `Workbench`. The kernel is the deep, performance- and consistency-critical core.
- **Components** — the React component library on PrimeReact. `Source/<Component>/` folder per component (`CommandDialog`, `DataPage`, `DataTables`, …) with Storybook stories; published as `@cratis/components`. (Application rules *consume* these components; here you *build* them.)

Repo conventions follow from this: `Source/DotNET` + `Source/JavaScript` for dual-stack libraries; `Kernel` vs `Clients` for Chronicle; a folder-per-component library layout for Components. Match the structure of the area you are editing — do not introduce app-style layouts.

## Framework-contributor principles

- **Public API design is the product.** These libraries are consumed by every Cratis app, so the **Lovable APIs** value (`general.md`) is paramount: sane defaults, convention over configuration, extensible/overridable, minimal boilerplate. Design the API the app developer will love before the implementation.
- **Backward compatibility is a contract.** A change to a public type, attribute, interface, or generated-proxy shape is a breaking change for every downstream app — label PRs by semver impact (`major`/`minor`/`patch`) and treat removals/renames of public surface as `major`.
- **Convention discovery is built here.** `IInstancesOf<T>` / `IImplementationsOf<T>`, attribute-based discovery, and source generation are the mechanisms the framework *provides*; use them internally too rather than hand-registration where a convention fits.
- **Source generators / analyzers** (Arc's proxy generator, Fundamentals) follow Roslyn conventions; their output is consumed verbatim by apps, so treat generated shape as public API.
- **Orleans grains** in the Chronicle kernel follow `orleans.md`.
- **Specs** use `Cratis.Specifications` (the `Establish`/`Because`/`should_` BDD style) with NSubstitute — the same philosophy as apps, without the Arc/Chronicle `*Scenario` app-testing helpers.

## Quality gates (framework)

- Build clean (Debug and Release) with **zero warnings, zero errors**.
- Specs pass for affected projects (C# via `dotnet test`; TS packages via their test command; Components also `build-storybook`).
- For public-facing changes (APIs, attributes, generated output, component props): update the product documentation and verify it.
- Match the repo's existing patterns; when a deep architectural question isn't answered by the universal rules or this file, consult the **repo's own docs/CONTRIBUTING** or ask — do not infer framework internals from a single call site.

## Depth lives in the repo

This file is the cross-cutting framework-contributor baseline. Repo-specific internals — the Chronicle kernel's grain/observer/storage design, Arc's proxy-generator internals, the Components build pipeline — are owned by each repo's own documentation, not duplicated here. Follow those for area-specific detail.
