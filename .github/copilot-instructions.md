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

- When adding new instructions or rules, always place them in the most specific location that applies — the relevant `.instructions.md` file (e.g. `typescript.instructions.md`, `csharp.instructions.md`) or a `SKILL.md` if it relates to a specific workflow. Only add something to `copilot-instructions.md` if it genuinely applies to every file and every context in the project.
- Always use American English spelling in all code, comments, and documentation (e.g. "color" not "colour", "behavior" not "behaviour").
- Write clear and concise comments for each function.
- Make only high confidence suggestions when reviewing code changes.
- Never change global.json unless explicitly asked to.
- Never change package.json or package-lock.json files unless explicitly asked to.
- Never change NuGet.config files unless explicitly asked to.
- Always ensure that the code compiles without warnings. **Warnings are treated as errors** — a build with warnings is a failing build.
- Always ensure that the code passes all tests.
- Always ensure that the code adheres to the project's coding standards.
- Always ensure that the code is maintainable.
- Always reuse the active terminal for commands.
- Do not create new terminals unless current one is busy or fails.
- **A task is not complete until the code has been built and every warning and error has been resolved.** Never hand back to the user with outstanding build warnings or errors.

## Development Workflow

- After creating each new file, run `dotnet build` (C#) or `yarn compile` (TypeScript) immediately before proceeding to the next file. Fix all errors **and warnings** as they appear — never accumulate technical debt.
- Before marking any task done, run a final build (`dotnet build` for C#, `yarn compile` for TypeScript) and confirm it exits with zero errors and zero warnings. If there are warnings or errors, fix them before considering the task complete.
- Before adding parameters to interfaces or function signatures, review all usages to ensure the new parameter is needed at every call site.
- When modifying imports, audit all occurrences — verify additions are used and removals don't break other files.

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
   - [Git Commits](./instructions/git-commits.instructions.md)
   - [Pull Requests](./instructions/pull-requests.instructions.md)
   - [Vertical Slices](./instructions/vertical-slices.instructions.md)
   - [TypeScript Conventions](./instructions/typescript.instructions.md)
   - [React Components](./instructions/components.instructions.md)
   - [Dialogs](./instructions/dialogs.instructions.md)
   - [Reactors](./instructions/reactors.instructions.md)
   - [Orleans](./instructions/orleans.instructions.md)

## Header

All files should start with the following header:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```
