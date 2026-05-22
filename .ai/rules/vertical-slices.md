---
applyTo: "**/Features/**/*.*"
---

# Vertical Slice Architecture

A vertical slice contains *everything* for a single behavior: the command or query, the events it produces, the projections that build read models, the React component that renders the UI, and the specs that verify it all works. Everything lives together in one folder because everything changes together.

## Technical Stack

- .NET with C# 13 (ASP.NET Core) — Cratis Arc for CQRS, Cratis Chronicle for event sourcing, MongoDB for read models
- React + TypeScript (Vite) — PrimeReact UI, Vitest + Mocha/Chai/Sinon for specs
- xUnit + Cratis.Specifications + NSubstitute for C# specs

## Core Rules

These are non-negotiable because the frameworks rely on them for convention-based discovery and proxy generation:

- **Each vertical slice has its own folder with a single `.cs` file containing ALL backend artifacts.**
- **Commands define `Handle()` directly on the record — never create separate handler classes.**
- **`[EventType]` must have NO arguments — the type name is used automatically.**
- Complete one slice end-to-end before starting the next.
- Drop the `.Features` segment from namespaces (e.g. `MyApp.Projects.Registration` not `MyApp.Features.Projects.Registration`).

---

## Proxy Generation — Build Dependency

`dotnet build` generates TypeScript proxies. Until the backend compiles, **no proxy files exist** and frontend code cannot reference them.

**Sequencing constraint:** Backend → `dotnet build` → Frontend. Backend and frontend for the same slice **cannot** run in parallel.

---

## Slice Types

| Type | Purpose | What It Contains |
|---|---|---|
| **State Change** | Mutates system state | Command + events + validators/constraints |
| **State View** | Projects events into queryable read models | Read model + projection + queries |
| **Automation** | Reacts to events, makes decisions | Reactor + local read models |
| **Translation** | Adapts events across slices/systems | Reactor → triggers commands in own slice |

---

## Folder Structure

```
Features/
├── <Feature>/
│   ├── <Feature>.tsx              ← composition page (layout + menu)
│   ├── <Concept>.cs               ← shared concepts for this feature
│   ├── <SliceName>/
│   │   ├── <SliceName>.cs         ← ALL backend artifacts in ONE file
│   │   ├── <Component>.tsx        ← React component(s) for this slice
│   │   └── when_<behavior>/       ← integration specs (state-change slices)
│   │       ├── and_<scenario>.cs
│   │       └── ...
│   └── ...
```

**✅ CORRECT:**
```
Features/Authors/
├── Authors.tsx
├── AuthorId.cs
├── Registration/
│   ├── Registration.cs            ← command + event + constraint
│   ├── AddAuthor.tsx
│   └── when_registering/
│       └── and_name_already_exists.cs
└── Listing/
    ├── Listing.cs                 ← read model + projection + query
    └── Listing.tsx
```

**❌ WRONG — Never split by artifact type:**
```
Features/Authors/
├── Commands/RegisterAuthor.cs
├── Handlers/RegisterAuthorHandler.cs
└── Events/AuthorRegistered.cs
```

---

## What Goes in a Single Slice File

A single `<SliceName>.cs` contains ALL of: `[Command]` records with `Handle()`, validators, constraints, `[EventType]` records, `[ReadModel]` records with static query methods, projections/reducers, reactors, and slice-specific concepts.

---

## Events — Rules

- `[EventType]` takes **no arguments** — the type name is the identifier.
- Past tense naming: `AuthorRegistered`, `BookReserved`, `AddressChanged`.
- Never nullable properties — if something is optional, you need a second event.
- One purpose per event — never multipurpose events with many nullable fields.

```csharp
[EventType]
public record AuthorRegistered(AuthorName Name);
```

---

## Commands — Rules

- `[Command]` record from `Cratis.Arc.Commands.ModelBound`.
- `Handle()` defined directly on the record — no separate handler class.
- `Handle()` returns: single event, tuple `(event, result)`, `Result<TSuccess, TError>`, or `void`.
- Event source resolved from: `[Key]` parameter → `EventSourceId`-convertible type → `ICanProvideEventSourceId`.
- Business rules via DCB: accept a read model parameter in `Handle()` — the framework injects current state.

→ For step-by-step command creation, invoke the **`cratis-command`** skill.

---

## Read Models & Projections — Rules

- Prefer model-bound attributes (`[ReadModel]`, `[FromEvent<T>]`, `[Key]`, etc.) over fluent `IProjectionFor<T>`.
- AutoMap is **on by default** — call `.From<EventType>()` directly, no `.AutoMap()` call needed.
- Projections join **events**, never read models.
- Query methods are **static** methods on the `[ReadModel]` record.
- Favor reactive queries (`ISubject<T>`) for real-time updates.

→ For step-by-step read model creation, invoke the **`cratis-readmodel`** skill.
→ For adding a projection to an existing model, invoke the **`add-projection`** skill.

---

## Concepts — Rules

- Prefer `ConceptAs<T>` over raw primitives everywhere in domain models, commands, events, and queries.
- Concepts shared between slices → feature folder. Shared between features → `Features/` root. One file per concept.

→ See [concepts.instructions.md](./concepts.instructions.md) for full rules.
→ For step-by-step concept creation, invoke the **`add-concept`** skill.

---

## Reactors — Rules

- `IReactor` is a marker interface — method dispatch is by first-parameter event type.
- Reactors observe events and produce side effects — never use `IEventLog` directly from a reactor.
- If a reactor needs to write new events, execute a command via `ICommandPipeline`.
- Design for idempotency — reactors may be called more than once.

→ See [reactors.instructions.md](./reactors.instructions.md) for full rules.
→ For step-by-step reactor creation, invoke the **`add-reactor`** skill.

---

## Development Workflow

Work **slice-by-slice** in this exact order:

1. **Backend** — implement the C# slice file
2. **Specs** — write integration specs for state-change slices
3. **Build** — run `dotnet build` to generate TypeScript proxies
4. **Frontend** — implement React component(s) using the generated proxies
5. **Composition** — register in the feature's composition page
6. **Routes** — add/update routing if needed

→ For end-to-end slice implementation, invoke the **`new-vertical-slice`** skill.
→ For creating a new feature folder, invoke the **`scaffold-feature`** skill.

---

## Dialogs — Rules

- **Never** import `Dialog` from `primereact/dialog` — use Cratis wrappers.
- `CommandDialog` from `@cratis/components/CommandDialog` — for dialogs that execute commands.
- `Dialog` from `@cratis/components/Dialogs` — for data collection without commands.

→ See [dialogs.instructions.md](./dialogs.instructions.md) for full dialog patterns.

