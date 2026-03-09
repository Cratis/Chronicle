---
applyTo: "**/Features/**/*.*"
---

# Vertical Slice Architecture — Critical Rules

These rules are enforced by the framework via convention-based discovery. Breaking them causes silent runtime failures, not compile errors.

## Non-negotiable

- **All backend artifacts for a slice go in a single `<SliceName>.cs` file** — command, event, validator, constraint, read model, projection, all together.
- **Commands define `Handle()` directly on the `[Command]` record** — never create a separate handler class. Arc discovers `Handle()` by convention.
- **`[EventType]` takes NO arguments** — the type name is the identifier. `[EventType("name")]` and `[EventType("guid")]` are both wrong.
- **Projections join EVENTS, never read models** — a projection that reads another read model breaks replay and creates hidden dependencies.
- **Event properties are never nullable** — if a property is optional, you need a second event.
- **Namespace drops the `.Features.` segment** — `MyApp.Authors.Registration`, not `MyApp.Features.Authors.Registration`.
- **Never import `Dialog` from `primereact/dialog`** — use `CommandDialog` from `@cratis/components/CommandDialog` or `Dialog` from `@cratis/components/Dialogs`.

## Folder structure

```
Features/<Feature>/<Slice>/<Slice>.cs        <- all backend
Features/<Feature>/<Slice>/<Component>.tsx   <- React component
Features/<Feature>/<Slice>/when_<behavior>/  <- integration specs
```

For step-by-step implementation guidance, use the `new-vertical-slice` skill.
For architecture explanations and slice type selection, use the `cratis-vertical-slice` skill.

## Sub-agent coordination

Backend and frontend for the **same slice cannot run in parallel** — the frontend agent must wait for `dotnet build` to complete. Steps 1–3 (backend) are a hard prerequisite for step 4 (frontend). Independent slices with no shared events can have their backends worked on in parallel, but each slice's frontend still depends on its own build completing first.
