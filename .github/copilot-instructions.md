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

## Formatting

- Honor the existing code style and conventions in the project.
- Apply code-formatting style defined in .editorconfig.
- Prefer file-scoped namespace declarations and single-line using directives.
- Insert a new line before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Place private class declarations at the bottom of the file.

## C# Instructions

- Prefer `var` over explicit types when declaring variables.
- Do not add unnecessary comments or documentation.
- Use `using` directives for namespaces at the top of the file.
- Sort the `using` directives alphabetically.
- Use namespaces that match the folder structure.
- Remove unused `using` directives.
- Use file-scoped namespace declarations.
- Use single-line using directives.
- For types that do not have an implementation, don't add a body (e.g., `public interface IMyInterface;`).
- Prefer using `record` types for immutable data structures (events, commands, read models, concepts).
- Use expression-bodied members for simple methods and properties.
- Use `async` and `await` for asynchronous programming.
- Use `Task` and `Task<T>` for asynchronous methods.
- Use `IEnumerable<T>` for collections that are not modified.
- Never return mutable collections from public APIs.
- Don't use regions in the code.
- Never add postfixes like Async, Impl, etc. to class or method names.
- Favor collection initializers and object initializers.
- Use string interpolation instead of string.Format or concatenation.
- Favor primary constructors for all types.

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

## TypeScript / Frontend Instructions

- Prefer `const` over `let` over `var` when declaring variables.
- Never use shortened or abbreviated names for variables, parameters, or properties.
  - Use full descriptive names: `deltaX` not `dx`, `index` not `idx`, `event` not `e`, `previous` not `prev`, `direction` not `dir`, `position` not `pos`, `contextMenu` not `ctx`/`ctxMenu`.
  - The only acceptable short names are well-established domain terms (e.g. `id`, `url`, `min`, `max`).
- Never leave unused import statements in the code.
- Always ensure that the code compiles without warnings.
  - Use `yarn compile` to verify (if successful it doesn't output anything).
- Do not prefix a file, component, type, or symbol with the name of its containing folder or the concept it belongs to. Instead, use folder structure to provide that context.
- Favor functional folder structure over technical folder structure.
  - Group files by the feature or concept they belong to, not by their technical role.
  - Avoid folders like `components/`, `hooks/`, `utils/`, `types/` at the feature level.

## Development Workflow

- After creating each new file, run `dotnet build` (C#) or `yarn compile` (TypeScript) immediately before proceeding to the next file. Fix all errors as they appear — never accumulate technical debt.
- Before adding parameters to interfaces or function signatures, review all usages to ensure the new parameter is needed at every call site.
- When modifying imports, audit all occurrences — verify additions are used and removals don't break other files.
- Never use placeholder or temporary types — use proper types from the start.
- Review each file for lint compliance before moving on.
- The user may keep Storybook running — do not try to stop it, suggest stopping it, or start your own instance.

## XML Documentation

- All **public** types, methods, properties, and operators **must** have XML doc comments.
- Always use **multiline** `<summary>` tags — opening and closing tags on their own lines. Never single-line.
- Every method or operator with parameters must include `<param name="...">` for each parameter.
- Every non-void method or operator must include `<returns>`.
- Every method that throws must document the exception with `<exception cref="...">` tags.
- Use `<see cref="..."/>` and `<paramref name="..."/>` to cross-reference types and parameters.

## Exceptions

- Use exceptions for exceptional situations only.
- Don't use exceptions for control flow.
- Always provide a meaningful message when throwing an exception.
- Always create a custom exception type that derives from Exception.
- Never use any built-in exception types like InvalidOperationException, ArgumentException, etc.
- Add XML documentation for exceptions being thrown.
- XML documentation for exception should start with "The exception that is thrown when ...".
- Never suffix exception class names with "Exception".

## Nullable Reference Types

- Always use is null or is not null instead of == null or != null.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.
- Add `!` operator where nullability warnings occur.
- Use `is not null` checks before dereferencing potentially null values.

## Naming Conventions

- Follow PascalCase for component names, method names, and public members.
- Use camelCase for private fields and local variables.
- Prefix private fields with an underscore (e.g., `_privateField`).
- Prefix interface names with "I" (e.g., IUserService).

## Logging

- Use structured logging with named parameters.
- Use appropriate log levels (Information, Warning, Error, Debug).
- Always use a generic ILogger<T> where T is the class name.
- Keep logging in separate partial methods for better readability. Call the file `<SystemName>Logging.cs`. Make this class partial and static and internal and all methods should be internal.
- Use the `[LoggerMessage]` attribute to define log messages.
- Don't include `eventId` in the `[LoggerMessage]` attribute.

## Dependency Injection

- Systems that have a convention of IFoo to Foo does not need to be registered explicitly.
- Prefer constructor injection over method injection.
- Avoid service locator pattern (i.e., avoid using IServiceProvider directly).
- For implementations that should be singletons, use the `[Singleton]` attribute on the class.

## TypeScript Type Safety

- Never use `any` type - always use proper type annotations:
  - Use `unknown` for values of unknown type that need runtime checking.
  - Use `Record<string, unknown>` for objects with unknown properties.
  - Use proper generic constraints like `<TCommand extends object = object>` instead of `= any`.
  - Use `React.ComponentType<Props>` for React component types.
- When type assertions are necessary, use `unknown` as an intermediate type:
  - Prefer `value as unknown as TargetType` over `value as any`.
  - For objects with dynamic properties: `(obj as unknown as { prop: Type }).prop`.
- For generic React components:
  - Use `unknown` as default generic parameter instead of `any`.
  - Example: `<TCommand = unknown>` not `<TCommand = any>`.
- For Storybook files:
  - Use `React.ComponentType<Record<string, never>>` for components with no props.
  - Always use `as unknown as` when converting component imports to avoid type mismatch errors.
  - Properly type story args instead of using `any`.
- For event handlers:
  - Be careful with React.MouseEvent vs DOM MouseEvent - they are different types.
  - React synthetic events: `React.MouseEvent<Element, MouseEvent>`.
  - DOM native events: `MouseEvent`.
  - Convert between them using: `nativeEvent as unknown as React.MouseEvent`.
  - Use `e.preventDefault?.()` instead of `(e as any).preventDefault?.()`.
- For library objects (PIXI, etc.):
  - Use proper library types when available.
  - Use specific property types: `{ canvas?: HTMLCanvasElement }` instead of `any`.
- When working with external libraries that have strict generic constraints:
  - Import necessary types (e.g., `Command` from `@cratis/arc/commands`).
  - Use type assertions through `unknown` to satisfy constraints: `props.command as unknown as Constructor<Command<...>>`.
  - Extract tuple results explicitly rather than destructuring when type assertions are needed.
- For function parameter types that may be unknown:
  - Add type guards: `if (typeof accessor !== 'function') return ''`.
  - Type parameters with fallbacks: `function<T = unknown>(accessor: ((obj: T) => unknown) | unknown)`.
- For arrays and collections accessed from `unknown` types:
  - Cast to proper array type: `((obj as Record<string, unknown>).items || []) as string[]`.
  - Type array elements when iterating: `array.forEach((item: string) => ...)`.
- For generic type parameters:
  - Ensure proper type conversions: `String(value)` when string operations are needed.
  - Use explicit Date parameter types: `new Date(value as string | number | Date)`.

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

## Header

All files should start with the following header:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```
