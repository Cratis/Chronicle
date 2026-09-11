---
name: cratis-event-model-diagram
description: Create and maintain Mermaid eventmodeling diagrams for a Cratis module or feature, and keep them in sync with the code. Use when adding, renaming, moving, or deleting a module, feature, behavior, command, event, read model, automation, translation, or cross-module flow. Do not use to decide the event vocabulary or stream boundaries - settle the model first, then render it here.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-event-model-diagram/SKILL.md -->

# Event model diagrams

Render an already-decided event model as a Mermaid `eventmodeling` diagram that
lives beside the code and is updated in the same change. If the event vocabulary
or the stream boundaries are not yet decided, model first; this skill draws a
chosen model, it does not choose one.

## Source authority

Diagrams use **Mermaid's native `eventmodeling` diagram type** (Mermaid v11.15
and later). The grammar is owned by Mermaid, and
<https://mermaid.js.org/syntax/eventmodeling.html> is the authority whenever this
file and Mermaid disagree. Confirm the Mermaid version available to the renderer
before relying on a construct.

The Cratis artifacts a diagram refers to are verified against `Cratis.Chronicle`
`16.45.2` and the Arc query and command surface it is used with.

## What an event model is

It arranges a module's commands, events, read models, and automations on a
left-to-right business-flow timeline, answering *"what happens in this module,
and in what order?"*.

One file per module or feature, alongside the code, with the diagram in a fenced
`mermaid` block. A system-overview file at the source root shows only
cross-module flows.

## Grammar cheat sheet

**Frame prefix:** `tf` (timeframe — auto-connects to the previous frame) and
`rf` (resetframe — breaks the chain; start each independent flow with it). `rf`
*replaces* `tf`; `tf N rf Name` is invalid.

**Frame:** `<prefix> <number> <type> <EntityName>`. The number is unique;
declaration order does not matter because frames position by reference.

| Type | Swimlane | Represents |
| --- | --- | --- |
| `ui` | UI / Automation | the persona interacting — a persona name, never a screen name |
| `pcr` | UI / Automation | a reactor or automation processor |
| `cmd` | Command / Read Model | a command |
| `rmo` | Command / Read Model | a read model |
| `evt` | Events | an event type, using the exact self-describing type name |

**Multiple sources (`->>`).** A read model or fan-in reactor fed by several
frames references them by frame number:

```text
tf 10 rmo <ReadModelName> ->> 03 ->> 06 ->> 09
```

**Namespaces.** A `Module.` prefix creates a sub-swimlane. Use it for
cross-module entities and throughout the system overview.

**Comments.** `%% -- Section --`. A section header is a comment, never a frame.

## Behavior type to pattern

```text
%% -- State change: <CommandName> --------------------
rf 01 ui <Persona>
tf 02 cmd <CommandName>
tf 03 evt <EntityName><PastTenseVerb>

%% -- State view: <ReadModelName> (consumed by <Persona>) --
rf 04 rmo <ReadModelName> ->> 03
tf 05 ui <Persona>

%% -- Automation: <ReactorName> (external side effect only) --
rf 06 evt <EntityName><PastTenseVerb>
tf 07 pcr <ReactorName>

%% -- Translation: <Source>.<Event> -> <Target>.<Reactor> --
rf 08 evt <SourceModule>.<EventName>
tf 09 pcr <TargetModule>.<ReactorName>
tf 10 evt <TargetModule>.<EventName>
```

- A state view's `rmo` references the event frames it projects from by number;
  its consumer `ui` frame auto-chains after it. A passive read model has no
  consumer UI — emit only the `rmo ... ->>` line with a `%% passive` comment.
- Translation flows are reactor-only: `evt` to `pcr` to `evt`, with no
  intermediate `cmd`. If you draw a `cmd` between the `pcr` and the resulting
  event, it is an **automation**, not a translation — reclassify it.
- Several consumers of one read model: declare each consumer `ui` as its own
  `rf` frame with an explicit `->>` back to the `rmo`.

## The command rules table

Mermaid's eventmodeling grammar has no shape for validation or guards. After the
diagram, add a `## Command rules` section: a table with **Command**, **Rules**,
and **Emits / result**, summarizing validator, value-invariant, provider,
concurrency, and authorization rules plus no-op and diff behavior, in human
language. Include commands that emit no event.

## Process

1. **Discover** the behaviors by scanning the module for the artifacts that
   define them: command records for state changes, read models without a command
   handler for state views, `IReactor` implementations that call out of the
   system for automations, and `IReactor` implementations that return events for
   translations. Use the exact type names.
2. **Order** frames by domain causality — what must happen before what. Put state
   views after the events they project from. Use `rf` only between independent
   flows, never between sibling events of one flow, or the diagram becomes a
   tall tower one event wide.
3. **Write** the diagram and the command-rules table.
4. **Verify** it renders without a syntax-error banner, then reconcile it against
   the source: every behavior appears, classified by the marker it actually
   contains. A clean render proves valid Mermaid, not completeness — close the
   gaps against the code, never from memory.

## Common mistakes

- **A command that emits several events** chains them with consecutive
  `tf ... evt ...` frames. Do not fight the auto-chain with `rf`.
- **The same event emitted from several commands** gets its own `tf evt` frame at
  each emit point. Do not merge them into one.
- **Several consumers of one read model** — the first auto-chains; each
  additional consumer needs an explicit `rf <n> ui <Persona> ->> <rmo-number>`.
- **A screen name in the `ui` lane.** The `ui` lane is the persona.
- **A section header written as a frame.** Section headers are `%%` comments.
- **A cross-module flow updated in one place.** It needs three: the overview
  diagram, the source module's outputs, and the target module's inputs.

## Verify

- The diagram renders with no syntax-error banner.
- Every behavior in the module appears, with the type its code actually has.
- Every event frame uses the exact event type name from the source.
- Independent flows start with `rf`; sibling events within a flow do not.
- Passive read models have no consumer `ui` frame.
- The command-rules table covers every command, including those that emit
  nothing.
- Cross-module flows are reflected in all three places.
