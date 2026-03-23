# Code Review Checklists

## C# Architecture

- [ ] Each slice lives in a single file at `Features/<Feature>/<Slice>.cs`
- [ ] ALL backend artifacts in one file: command, validator, business rules, event, read model, projection, slice class
- [ ] No separate handler classes — `Handle()` is on the command `record` directly
- [ ] No shared mutable state between commands
- [ ] No service locator (`IServiceProvider` not injected as a dependency)
- [ ] No explicit singleton registration when `[Singleton]` attribute suffices
- [ ] Logging in a separate `*Logging.cs` partial file using `[LoggerMessage]`
- [ ] Namespace: `<NamespaceRoot>.<Feature>.<Slice>` (no `.Features.` segment)

## C# Commands

- [ ] `record` type, not `class`
- [ ] All properties use `init` (immutable)
- [ ] `Handle()` is the single public entry point
- [ ] No unnecessary constructors — primary constructor only
- [ ] Validation attributes on properties (`[Required]`, `[MaxLength]`, etc.) for simple rules
- [ ] Business rules that depend on Chronicle state use a read model parameter in `Handle()` (DCB pattern)

## C# Events

- [ ] `record` type with no mutable properties
- [ ] Decorated with `[EventType("stable-guid-string")]`
- [ ] No behaviour — data only
- [ ] Properties are domain types (Concepts), not raw primitives like `Guid` or `string`

## C# Read Models & Projections

- [ ] Read model is a `record` type
- [ ] Projection: AutoMap is on by default — `.AutoMap()` only needed after `.NoAutoMap()`
- [ ] No joins on the read model — joins are on Chronicle events only
- [ ] `ProjectionId` is a stable GUID string — never changes after first deployment
- [ ] No `ToList()`, `ToArray()`, or mutable collection exposed from public API

## C# Concepts

- [ ] Domain IDs/values use `ConceptAs<T>` (see `add-concept` skill)
- [ ] No raw `Guid`, `string`, `int` used where a concept should wrap it
- [ ] Concept has `static readonly NotSet`/`Empty` sentinel
- [ ] Concept has implicit conversion from primitive
- [ ] Concept has `New()` factory if Guid-backed

## C# Code Style

- [ ] File-scoped namespace declaration (`namespace Foo.Bar;`)
- [ ] `using` directives alphabetically sorted, no unused ones
- [ ] `is null` / `is not null` — never `== null` / `!= null`
- [ ] `var` preferred over explicit type declarations
- [ ] No postfixes on class names: `Async`, `Impl`, `Service`, `Manager`, `Helper`
- [ ] No regions (`#region`)
- [ ] No built-in exception types: `InvalidOperationException`, `ArgumentException`, etc.
- [ ] All public types, methods, and properties have multiline XML doc comments
- [ ] `<summary>` tags always multiline — never `/// <summary>Text</summary>` on one line
- [ ] Methods with parameters include `<param name="...">` for each parameter
- [ ] Non-void methods include `<returns>` documentation
- [ ] Methods that throw include `<exception cref="...">` documentation
- [ ] Custom exceptions derive from `Exception`, XML doc starts with "The exception that is thrown when …"
- [ ] Copyright header on every file
- [ ] No trailing whitespace or missing newlines at end of file

## TypeScript Architecture

- [ ] Components placed in the slice folder (`Features/<Feature>/<Slice>/`)
- [ ] No `index.ts` barrel just to re-export a single component
- [ ] No technical folder groupings (`hooks/`, `utils/`, `types/`) at feature level
- [ ] Feature folder structure is functional, not technical

## TypeScript Type Safety

- [ ] No `any` types — `unknown` with type guards
- [ ] No `(x as any)` — use `value as unknown as TargetType`
- [ ] React synthetic events and DOM events not confused (`React.MouseEvent` vs `MouseEvent`)
- [ ] Generic defaults use `unknown`, not `any` (e.g. `<T = unknown>`)
- [ ] No `@ts-ignore` or `@ts-expect-error` without a comment explaining why

## TypeScript Styling

- [ ] No hard-coded hex/rgb values — PrimeReact CSS variables (`var(--...)`) only
- [ ] CSS co-located with component (`.css` file in same folder)
- [ ] No `!important` unless justified with a comment

## TypeScript Code Style

- [ ] `const` over `let`, `let` over `var`
- [ ] Full descriptive names — never `e`, `evt`, `idx`, `i`, `prev`, `dir`, `pos`, `ctx`
- [ ] No async functions that don't `await` anything
- [ ] No unused variables or imports
- [ ] String enums for all enumerations (not numeric)
- [ ] Copyright header on every file

## Component Conventions

- [ ] `CommandDialog` from `@cratis/components/CommandDialog` for command-based dialogs
- [ ] `Dialog` from `@cratis/components/Dialogs` for data-only dialogs
- [ ] Never imports `Dialog` from `primereact/dialog` directly
- [ ] No monolithic components — decomposed into focused sub-components
- [ ] `README.md` exists for component folders with ≥2 sub-components or non-trivial architecture

## Spec Coverage

- [ ] Every State Change command has specs
- [ ] Happy path covered
- [ ] Every validation rule has a failure spec
- [ ] Every business rule violation has a spec
- [ ] Every constraint violation has a spec
- [ ] No spec for trivial property getters or constructor passthrough
