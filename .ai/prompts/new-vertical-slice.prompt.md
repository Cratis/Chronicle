---
agent: agent
description: Scaffold a complete vertical slice (backend + specs + frontend) for a Cratis-based project.
---

# New Vertical Slice

I need you to implement a complete **vertical slice** for a Cratis-based project.

## Inputs

Please provide (or I'll ask):
- **Feature name** — e.g. `Projects`
- **Slice name** — e.g. `Registration`, `Listing`, `Removal`
- **Slice type** — one of:
  - `State Change` — command that appends events and changes read models
  - `State View` — query that reads from a read model
  - `Automation` — background reactor triggered by events
  - `Translation` — transforms/enriches events into other events
- **Domain description** — what the slice does in plain language
- **Properties** — fields on the command/query and their types

## What I want produced

Follow `.github/instructions/vertical-slices.instructions.md` exactly.

### Phase 1 — Backend (delegate to `backend-developer` agent)

For a **State Change** slice, produce (in this order):
1. Concept types (if new strongly-typed IDs are needed)
2. Command `record` with `Handle()` method, validation, and business rules
3. Event `record` with `[EventType]` attribute (no arguments)
4. Read model `record` with `[ReadModel]` and model-bound projection attributes (`[FromEvent<T>]`, `[Key]`, etc.)
   - Use fluent `IProjectionFor<T>` only when model-bound attributes don't fit

For a **State View** slice, produce:
1. Read model `record` with `[ReadModel]`, model-bound projection attributes (`[FromEvent<T>]`, `[Key]`, etc.), and a static query method
   - Use fluent `IProjectionFor<T>` only when model-bound attributes don't fit

Run `dotnet build` after creating the `.cs` file. Fix all errors before proceeding.

### Phase 2 — Specs (delegate to `spec-writer` agent, State Change slices only)

For each command, write specs covering:
- Happy path (command succeeds, correct event appended)
- Each validation failure
- Each business rule violation
- Each constraint violation

Run `dotnet test` and fix all failures before proceeding.

### Phase 3 — Frontend (delegate to `frontend-developer` agent)

1. Create React component(s) in `Features/<Feature>/<Slice>/`
2. Use auto-generated proxy types from `dotnet build`
3. Use `CommandDialog` for command-based dialogs
4. Update the feature composition page
5. Update routing if a new page is introduced

Run `yarn lint` and `npx tsc -b`. Fix all errors before proceeding.

### Phase 4 — Quality Gates

- `dotnet build` — zero errors/warnings
- `dotnet test` — zero failures
- `yarn lint` — zero errors
- `npx tsc -b` — zero errors

## Constraints

- Namespace: `<NamespaceRoot>.<Feature>.<Slice>` — detect `<NamespaceRoot>` from existing source files
- No abbreviations in TypeScript
- No hard-coded colours — PrimeReact CSS variables only
- Copyright header on every file
- README.md for complex component folders
