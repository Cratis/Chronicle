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

- **Always use American English spelling** in all code, comments, documentation, and XML docs — no exceptions.
  - `-ize` not `-ise`: initialize, serialize, customize, normalize, organize, authorize, specialize, centralize, utilize
  - `-or` not `-our`: behavior, color, favor, honor, humor, neighbor, flavor
  - `-ization` not `-isation`: initialization, serialization, customization, normalization, organization, authorization
  - `-er` not `-re`: center, fiber, meter
  - `-og` not `-ogue`: dialog, catalog, analog
  - `-ling` not `-lling`: modeling, signaling, labeling, canceling
  - `-ense` not `-ence`: license, defense, offense
  - `-ment` not `-ement`: judgment, acknowledgment
  - Other: gray (not grey), program (not programme), fulfill (not fulfil), enroll (not enrol)
  - When in doubt, use the US spelling — check a US dictionary.
- Write clear and concise comments for each function.
- Make only high confidence suggestions when reviewing code changes.
- Never change global.json unless explicitly asked to.
- Never change package.json or package-lock.json files unless explicitly asked to.
- Never change NuGet.config files unless explicitly asked to.
- Always ensure that the code compiles without warnings.
- Always ensure that the code passes all tests.
- Always ensure that the code adheres to the project's coding standards.
- Always ensure that the code is maintainable.
- For PR descriptions, use short release-note bullets and append the **actual** issue number only when the PR is associated with a real GitHub issue (for example `(#351)`). If there is no associated issue, omit the reference entirely. Never use placeholder text like `(#issue)`, never leave the literal example `(#123)`, and never invent a random issue number. Never include Copilot "Original prompt" blocks.
- Always reuse the active terminal for commands.
- Do not create new terminals unless current one is busy or fails.
- When asked to commit, push, create a PR, ship, or land changes, always use the **ship-changes** skill.

## Development Workflow

- After creating each new file, run `dotnet build` (C#) or `yarn compile` (TypeScript) immediately before proceeding to the next file. Fix all errors as they appear — never accumulate technical debt.
- Before adding parameters to interfaces or function signatures, review all usages to ensure the new parameter is needed at every call site.
- When modifying imports, audit all occurrences — verify additions are used and removals don't break other files.
- At the end of every task, from repository root run `dotnet clean` and then `dotnet build -c Release`. The task is not complete until build output is zero warnings and zero errors.

## Detailed Guides

These guides contain the full rules, examples, and rationale for each topic. The sections above are the global defaults; the guides go deeper into each area:
   - [Code Quality](./instructions/code-quality.instructions.md)
   - [Code Quality — C#](./instructions/code-quality.csharp.instructions.md)
   - [Code Quality — TypeScript](./instructions/code-quality.typescript.instructions.md)
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

## Header

All files should start with the following header:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```
