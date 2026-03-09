# GitHub Copilot Instructions

## Project Philosophy

Cratis builds tools for event-sourced systems with a focus on **ease of use**, **productivity**, and **maintainability**. Every rule in these instructions serves one or more of these core values:

- **Lovable APIs** — APIs should be pleasant to use. Provide sane defaults, make them flexible, extensible, and overridable. If an API feels awkward, it is wrong.
- **Easy to do things right, hard to do things wrong** — Convention over configuration. Artifact discovery by naming. Minimal boilerplate. The framework should guide developers into the pit of success.
- **Events are facts** — Immutable records of things that happened. Never nullable, never ambiguous, never multipurpose. If you find yourself adding a nullable property to an event, you need a second event.
- **High cohesion through vertical slices** — Everything for a behavior lives together: backend, frontend, specs. Navigate by feature, not by technical layer. A developer working on "author registration" should never need to jump between `Commands/`, `Handlers/`, and `Events/` folders.
- **Full-stack type safety** — Shared models flow from C# through proxy generation to TypeScript. End-to-end typing without manual synchronization.
- **Specialization over reuse** — Build focused, purpose-specific projections and read models rather than reusing one model across conflicting scenarios. Dedicated models are easier to maintain, perform better, and never break unrelated features.
- **Consistency is king** — When in doubt, follow the established pattern. Consistency across the codebase trumps local optimization. A slightly less elegant solution that matches the rest of the codebase is better than a clever one that stands out.

When these instructions don't explicitly cover a situation, apply these values to make a judgment call.

## General

- Always use American English spelling in all code, comments, and documentation (e.g. "color" not "colour", "behavior" not "behaviour").
- Write clear and concise comments for each function.
- Make only high confidence suggestions when reviewing code changes.
- Always use the latest version C#, currently C# 13 features.
- Never change global.json unless explicitly asked to.
- Never change package.json or package-lock.json files unless explicitly asked to.
- Never change NuGet.config files unless explicitly asked to.
- Never leave unused using statements in the code.
- Always ensure that the code compiles without warnings.
- Always ensure that the code passes all tests.
- Always ensure that the code adheres to the project's coding standards.
- Always ensure that the code is maintainable.
- Review Directory.Build.props and .editorconfig for all warnings configured as errors.
- Never generate code that would violate these warning settings.
- Always respect the project's nullable reference type settings.
- Always reuse the active terminal for commands.
- Do not create new terminals unless current one is busy or fails.

## Chronicle & Arc Key Conventions

These conventions exist because the frameworks rely on convention-based discovery. Breaking them doesn't just violate style — it breaks runtime behavior.

- `[EventType]` takes **no arguments** — the type name is used automatically. Adding a GUID or string argument is a common mistake from other frameworks; Chronicle does not use it.
- `[Command]` records define `Handle()` directly on the record — never create separate handler classes. The framework discovers `Handle()` by convention; a separate class breaks that discovery.
- `[ReadModel]` records define static query methods directly on the record. The proxy generator creates TypeScript hooks from these methods automatically.
- Prefer `ConceptAs<T>` over raw primitives in all domain models, commands, events, and queries. This prevents accidental mix-ups (passing a `UserId` where an `AuthorId` was expected) and makes APIs self-documenting.
- Use model-bound projection attributes (`[FromEvent<T>]`, `[SetFrom<T>]`, etc.) when possible; fall back to `IProjectionFor<T>` for complex cases.
- For fluent projections, AutoMap is on by default — just call `.From<>()` directly.
- Projections join **events**, never read models. This is fundamental to event sourcing: projections rebuild state from the event stream, not from other projections.
- `IReactor` is a marker interface — method dispatch is by first-parameter event type, method name is descriptive.
- All backend artifacts for a vertical slice go in a **single `.cs` file**. This keeps cohesion high and makes the scope of a slice immediately visible.

## Proxy Generation — Build Dependency

Commands and Queries generate TypeScript proxies at build time via `dotnet build`. This creates `.ts` files that the frontend imports (hooks, execute methods, change tracking). Until the backend compiles, **no proxy files exist** and frontend code cannot reference them.

**This is a hard sequencing constraint:**
1. Backend C# code must be written and compile successfully first.
2. `dotnet build` must complete — this generates the TypeScript proxy files.
3. Only then can frontend React components import and use the generated proxies.

**When running parallel agents or sub-agents:** backend and frontend work for the same slice **cannot** run in parallel. The backend agent must finish and `dotnet build` must succeed before the frontend agent starts. Independent slices (no shared events) can have their backends worked on in parallel, but each slice's frontend still depends on its own backend build completing first.

## Development Workflow

- After creating each new file, run `dotnet build` (C#) or `yarn compile` (TypeScript) immediately before proceeding to the next file. Fix all errors as they appear — never accumulate technical debt.
- Before adding parameters to interfaces or function signatures, review all usages to ensure the new parameter is needed at every call site.
- When modifying imports, audit all occurrences — verify additions are used and removals don't break other files.
- Never use placeholder or temporary types — use proper types from the start.
- Review each file for lint compliance before moving on.
- The user may keep Storybook running — do not try to stop it, suggest stopping it, or start your own instance.

## TypeScript Type Safety

- Never use `any` — use `unknown`, `Record<string, unknown>`, or proper generic constraints instead. See [TypeScript Conventions](./instructions/typescript.instructions.md) for detailed patterns.

## Detailed Guides

These guides contain the full rules, examples, and rationale for each topic. The sections above are the global defaults; the guides go deeper into each area:
   - [C# Conventions](./instructions/csharp.instructions.md)
   - [How to Write Specs](./instructions/specs.instructions.md)
   - [How to Write C# Specs](./instructions/specs.csharp.instructions.md)
   - [How to Write TypeScript Specs](./instructions/specs.typescript.instructions.md)
   - [Entity Framework Core](./instructions/efcore.instructions.md)
   - [Entity Framework Core Specs](./instructions/efcore.specs.instructions.md)
   - [Concepts (ConceptAs)](./instructions/concepts.instructions.md)
   - [Documentation](./instructions/documentation.instructions.md)
   - [Pull Requests](./instructions/pull-requests.instructions.md)
   - [Vertical Slices](./instructions/vertical-slices.instructions.md)
   - [TypeScript Conventions](./instructions/typescript.instructions.md)
   - [React Components](./instructions/components.instructions.md)
   - [Dialogs](./instructions/dialogs.instructions.md)
   - [Reactors](./instructions/reactors.instructions.md)
   - [Orleans](./instructions/orleans.instructions.md)
