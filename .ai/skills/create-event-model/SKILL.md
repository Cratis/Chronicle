---
name: create-event-model
description: Create and maintain Mermaid eventmodeling diagrams (EventModel.md) for a Cratis module or feature. Use when adding, renaming, moving, or deleting modules/features/slices, commands, events, read models, automations, translations, or cross-module event flows — to keep the diagram in sync with the code.
---

# Create Event Model Diagram

Use this skill when adding a module/feature, or when any slice within it is added, renamed, or removed — the `EventModel.md` for the affected area is updated in the same change. If the event vocabulary/stream boundaries aren't decided yet, use `event-modeling` first; this skill renders an already-chosen model.

Diagrams use **Mermaid's native `eventmodeling`** diagram type (v11.15+). The full grammar is at <https://mermaid.js.org/syntax/eventmodeling.html>; that page wins when in doubt.

## What an event model is

It arranges a module's commands, events, read models, and automations on a left-to-right business-flow timeline — answering *"what happens in this module, and in what order?"* One file per module (or feature), in a fenced ` ```mermaid ` block, alongside the code: `<Module>/EventModel.md`; a system-overview at the source root shows only cross-module flows.

## Mermaid eventmodeling cheat sheet

**Frame prefix:** `tf` (timeframe — auto-connects to the previous frame) · `rf` (resetframe — breaks the chain; start each independent flow with it). `rf` *replaces* `tf`; `tf N rf Name` is invalid.

**Frame:** `<prefix> <number> <type> <EntityName>`. Number is unique (order of declaration doesn't matter — frames position by reference). Type is one of:

| Type | Swimlane | Represents |
|---|---|---|
| `ui` | UI / Automation | the persona interacting (persona name only — not a screen name) |
| `pcr` | UI / Automation | a reactor / automation processor |
| `cmd` | Command / Read Model | a `[Command]` record |
| `rmo` | Command / Read Model | a `[ReadModel]` record |
| `evt` | Events | an `[EventType]` record — use the exact, self-describing C# name |

**Multiple sources (`->>`):** a read model or fan-in reactor fed by several frames references them by **frame number**: `tf 10 rmo Profile ->> 03 ->> 06 ->> 09`. **Namespaces:** a `Module.` prefix (`tf 04 pcr Billing.CreateInvoice`) creates a sub-swimlane — use it for cross-module entities and in the system overview. **Comments:** `%% ── Section ──` (don't use a frame as a section header).

## Slice type → pattern

```
%% ── State Change: Register ──────────────
rf 01 ui <Persona>
tf 02 cmd Register
tf 03 evt <Entity>Registered

%% ── State View: <ReadModel> (consumed by <Persona>) ──
rf 04 rmo <ReadModel> ->> 03
tf 05 ui <Persona>

%% ── Automation: <Reactor> (side-effect only) ──
rf 06 evt <Entity>Registered
tf 07 pcr <Reactor>          %% calls an external service; emits no event

%% ── Translation: Source.Event -> Target.Reactor ──
rf 08 evt Source.SomethingHappened
tf 09 pcr Target.Reactor
tf 10 evt Target.SomethingElseHappened
```

- A State View's `rmo` references the event frames it projects from by number; its consumer-UI frame auto-chains after it. A `[Passive]` read model has no consumer UI — emit only the `rmo … ->>` line with a `%% passive` comment.
- Translation slices are reactor-only (`evt → pcr → evt`, no intermediate `cmd`). If you draw a `cmd` between `pcr` and the result event, it's an **Automation**, not a Translation — reclassify.
- Multiple consumers of one read model: declare each consumer UI as its own `rf` frame with an explicit `->>` back to the `rmo`.

## Command rules table

Mermaid eventmodeling has no shape for validation/guards. After the diagram, add a `## Command rules` section: a table with `Command`, `Rules`, `Emits / result`, summarizing validator/`ConceptValidator`/`Provide()`/DCB/authorization rules and no-op/diff behavior in human language. Include commands that emit no event (e.g. response-only parser commands).

## Process

1. **Discover** the slices: scan the module for `[Command]` (State Change), `[ReadModel]` without `Handle()` (State View), `IReactor` + `ICommandPipeline` (Automation), `IReactor` returning events / `IEventLog` (Translation). Use exact C# type names.
2. **Order** frames by domain causality (what must happen before what); State Views after the events they project; `rf` only between independent flows, not between sibling events of one flow (`rf`-per-event makes a tall tower).
3. **Write** the diagram + the `## Command rules` table.
4. **Verify** it renders without a syntax-error banner, then reconcile against the source: every slice `.cs` appears, classified by the marker it actually contains. A clean render proves valid Mermaid, not completeness — close gaps against the code, not from memory.

## Common mistakes

- **Multi-event State Change:** a command emitting several events chains them with consecutive `tf … evt …` — don't fight the auto-chain with `rf` (one event per row → tall tower).
- **Same event from multiple commands:** each emit-point gets its own `tf evt` frame; don't merge them into one.
- **Multiple consumers of one read model:** the first consumer auto-chains; each additional consumer references the read model explicitly (`rf <n> ui <Persona> ->> <readmodel-id>`).
- **Section headers** are Mermaid comments (`%%`), not frames; a screen/persona is the UI lane (`<Persona>`), never the screen name.
- **System overview:** when a flow crosses modules, update three places — the overview diagram, the source module's "Outputs to", and the target module's "Inputs from".

## See also

- `event-modeling` — decide the model before drawing it.
- `vertical-slices.md` — slice types and anatomy.
