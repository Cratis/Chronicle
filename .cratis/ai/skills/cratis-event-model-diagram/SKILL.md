---
name: cratis-event-model-diagram
description: Create and maintain Mermaid eventmodeling diagrams for a Cratis module or feature, and keep them in sync with their authority (the accepted `.play` model where one covers the scope, otherwise the code). Use when adding, renaming, moving, or deleting a module, feature, behavior, command, event, read model, automation, translation, or cross-module flow. Do not use to decide the event vocabulary or stream boundaries - settle the model first, then render it here.
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
`18.3.0` and the Arc query and command surface it is used with.

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

%% -- Automation: <ReactorName> (reacts to our own event) --
rf 06 evt <EntityName><PastTenseVerb>
tf 07 pcr <ReactorName>
%% add the next two frames only when the automation invokes a command
tf 08 cmd <CommandName>
tf 09 evt <EntityName><PastTenseVerb>

%% -- Translation: <Outside>.<Fact> -> <Adapter> -> our event --
rf 10 evt <ExternalSource>.<FactName>
tf 11 pcr <AdapterName>
tf 12 evt <EntityName><PastTenseVerb>
```

- A state view's `rmo` references the event frames it projects from by number;
  its consumer `ui` frame auto-chains after it. A passive read model has no
  consumer UI — emit only the `rmo ... ->>` line with a `%% passive` comment.
- One definition of Translation everywhere: data from **outside** our own facts
  becomes our own facts. Its flow starts at an outside-origin `evt` (an
  `<ExternalSource>.` prefix marks it), passes through the adapter `pcr`, and
  ends at an event we own. A `pcr` that reacts to **our own** event is an
  **automation**, whether it causes an external side effect, appends a
  follow-up event, or invokes a command (then draw the `cmd` and its resulting
  `evt` after the `pcr`). Classify by where the data comes from, not by how the
  reactor is built.
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
   system or return events or commands in response to our own events for
   automations, and adapters that take outside data in as our own events for
   translations. Choose the **authority** here: when an accepted model covers
   the scope (or the repository is opted in, per `cratis-screenplay-modeling-lifecycle`), read the behaviors from
   its slices (`StateChange`, `StateView`, `Automation`, `Translate`) and render
   from the model; otherwise the code is the authority. Report code that
   disagrees with the model as drift, separately; do not add unmodeled behavior
   to the diagram. Use the exact type names.
2. **Order** frames by domain causality — what must happen before what. Put state
   views after the events they project from. Use `rf` only between independent
   flows, never between sibling events of one flow, or the diagram becomes a
   tall tower one event wide.
3. **Write** the diagram and the command-rules table.
4. **Verify** it renders without a syntax-error banner, then reconcile it against
   the authority chosen in step 1 (the model's slices, or the code's markers):
   every behavior appears, classified by the type the authority gives it. A clean
   render proves valid Mermaid, not completeness — close the gaps against the
   authority, never from memory. List model/code drift in the report instead of
   resolving it in the diagram.

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
- Every behavior in the module appears, with the type its authority (the accepted `.play` slice, otherwise the code) gives it; drift is reported separately.
- Every event frame uses the exact event type name from the source.
- Independent flows start with `rf`; sibling events within a flow do not.
- Passive read models have no consumer `ui` frame.
- The command-rules table covers every command, including those that emit
  nothing.
- Cross-module flows are reflected in all three places.

## Route near misses

- An accepted model covers the scope (or the repository is opted in): it is the source for this
  diagram; do not redraw from code and never change the model to match a
  diagram. The diagram is a view of it.
- Deciding the model rather than drawing one: `cratis-chronicle-event-modeling`
  for a hand-written Chronicle implementation, or
  `cratis-screenplay-event-modeling` when the model is authored as a Screenplay
  `.play` document.
