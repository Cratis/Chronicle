---
name: cratis-screenplay-captures-and-reactions
description: Model the reactive edges of a Cratis Screenplay `.play` model — `capture` in the Change Data Capture Language for turning external data into events, and `reaction` plus `trigger` for work that runs when something happens, on a schedule, or on an integration signal. Use when integrating an external system, translating outside data into events, or declaring automatic follow-up work in a `.play` model. Do not use for commands, projections or screens.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-captures-and-reactions/SKILL.md -->

# Captures and reactions

Read [preferred syntax](../cratis-screenplay-toolchain/references/preferred-syntax.md) when writing or reviewing `.play`.

`capture` accepts outside data; `reaction` responds to an occurrence.
Translate slices cross public boundaries; Automation slices cause private follow-up.

## Locate the model

Look first for the project's existing `.play` files: their folder is the model.
For a new model, check `Source/` or `src/`, else use `Screenplay/` at the repository
root. Never put a model under `.cratis/` or hand-copy it between repositories.
`cratis ai install` manages `.cratis/ai/`, never model files. Keep explanatory
Markdown in project documentation; `.play` is the single flow model.

## Product sources

Language claims follow Screenplay `reactions.md`, `triggers.md`, `captures.md` and
`specifications.md` under `Documentation/screenplay/`; renderer claims follow Stage
`Documentation/reference/reaction-rendering.md`. Tool versions are covered in toolchain
`SKILL.md`; run the examples at the pinned standalone tool before relying on them.

The compiler's ESM admission decides, not the slice type. Reactions, triggers and
captures bind/reference-execute from v6; older compilers refuse them. Read toolchain
`references/diagnostics.md` when diagnosing skew or false PLAY0285 cascades.
- **Stage realization:** read `cratis-stage-rendering-and-sandbox` `references/admission.md`
  before claiming rendering. Supported event-triggered Automation renders; captures,
  Translate, clocks and opaque bodies remain gap-fill (`-024`).
  Read [sandbox boundaries](../cratis-stage-rendering-and-sandbox/references/sandbox-and-specrunner.md)
  when checking authored occurrence versus generated-spec limits; a clock fixture is not scheduling.

Name the binding tool. For the automation test, loops and translation boundaries,
read `cratis-screenplay-automations-and-translations`.

## Public boundaries and observer selection

Read [ESM v9 translation](../cratis-screenplay-automations-and-translations/references/public-events-and-direction.md)
for `direction`, `source events`/`from`, typed event fixtures and per-key state folds.
One `source events` capture consumes events from exactly one origin (event store).
Split inputs from several external systems into one capture per origin; otherwise
PLAY0687 reports an error, matching Chronicle's one-inbox-observer boundary.

Requires Screenplay 4.155.0 or later (`screenplay --version`) for PLAY0687; older compilers do not catch the runtime-invalid multi-origin capture.
Read [v10 filters](../cratis-screenplay-toolchain/references/production-routes-and-filters.md)
for reaction/reducer `from Source[.Stream]`; unrouted facts never match.

## Specify what sets them off

A specification names its input: `when clock` or `when trigger` for an automation,
`when capture` for a record source, and `when append <ForeignEvent>` for a capture
with `source events`. Seed the latter with `given <ForeignEvent>`, never `given capture`
or `when capture` (PLAY0701). Its key must be a required property of every `from`
event, never `$eventSourceId` or another context key (PLAY0700).

Requires Screenplay 4.171.0 or later (`screenplay --version`) for reference execution of captures over events and PLAY0700/0701; older tools leave event delivery unexecuted.

First sight is `added` only, not a field change. Read
[record capture specifications](references/record-capture-specifications.md) when
specifying first sight, updates, child/nested additions or unchanged records.
Execute admitted scenarios through `screenplay test` or MCP `run-specifications`;
source compilation alone is only authored evidence.

Read [references/specifying-triggers-and-clock.md](references/specifying-triggers-and-clock.md) when writing a specification for a reaction, trigger or capture, or when the clock, occurrence or limit rules matter.

## Atomic batches and event-source mappings

Start new models with application-root `reaction batches atomic`. One trigger's direct
productions, or one selected refusal branch, append all-or-none; invoked commands stay
separate. Legacy models without opt-in retain per-fact appends even at newer versions.
Read [atomic batches and source mappings](references/atomic-batches-and-source-mappings.md)
for batch failure assertions, PLAY0693, Stage atomic-mode refusals and
`invokes` mappings from `$eventSourceId` / `$eventContext.eventSourceId`.

## Caller-less invokes and unprotected reads

**Who is the caller of an `invokes`?** Without a declared actor, nobody.
Reactions support reaction-level `runs as system role "<Role>"`
(optional roles, joined with `and role`). It selects ESM v10 and supplies the
reference invocation's authenticated system principal with exactly those roles;
`given caller` does not supply it. Keep gates and declare the least-privilege actor.
Read [invocation identity](references/invocation-identity.md) for the checked
scenario, claim/unknown semantics and PLAY0647–0652/0557 diagnostics.

Requires Screenplay 4.125.0 or later (`screenplay --version`) for reaction `runs as system role` (ESM v10).

A compiler that stops before v10 rejects this source with PLAY0137, and a renderer that stops
before v10 refuses it (CLI-RENDER-004); the CLI/Stage pair (toolchain
`references/renderable-subset.md`) renders supported identity, refusing claim-reading/opaque gates (STAGE-ESM-034).
Stage emits Arc `[ExecuteCommandsAsSystem]`; refused scope uses authorized gap-fill. The attribute is for returned commands. Without that attribute
Arc supplies no principal and `[Authorize]`/`[Roles]` deny. Each invoked command
is atomic, not the whole cascade; earlier accepted facts remain after a later refusal.

⚠️ **Declared, not enforced.** A trigger `reads` gives no runtime protection. From
ESM v6 the binder refuses to treat it as one: a trigger with `reads` that **produces
directly**, or whose effect is an opaque body (`file` or an inline block), fails
binding, because its decision dependency cannot be protected. A reaction that only
`invokes` a command binds, and the decision, and any protection, belong to that
command's own reads. Eligible command reads protect under application-root
`command reads protected` (ESM v25); legacy reads retain PLAY0271. Read command-surface
[protected reads](../cratis-screenplay-command-surface/references/protected-reads.md)
for admission and absence/conflict behavior. Requires Stage 4.138.0 to render
protected reads; older bundles refuse -044. Semantic Host/executor non-Error
protected requirements refuse -045.
`where` over read paths out of scope, and `where` needs
every operand to resolve from the trigger's declared scalar occurrence shape (listed in
the input selection or not; nested paths and read aliases fail binding): keep the logic that uses the state in
the invoked command or the implementation. A compiler that stops before ESM v6 binds none of this
(`PLAY0268`).

## Refusals and recovery redelivery

Invocation refusal selectors, `$refusal` branch values and `acknowledge`
bind/reference-execute as ESM v19; `when redelivered ... to ...` and `then no events`
as ESM v17. Read [refusals and redelivery](references/refusals-and-redelivery.md)
when choosing ordered first-match branches or specifying recovery, for typed values,
occurrence matching, diagnostics and a complete example. Never acknowledge
infrastructure or concurrency failures; acknowledgement is not completed work.

## Complete example (ESM v6)

The reference document covers v6 triggers, event/clock reactions, view reads and
capture specifications. Refusal/redelivery forms use their separate checked reference.

Read [references/complete-examples.md](references/complete-examples.md) when you need a complete, compiling parent document for an excerpt or want to check the authoring and binding prerequisites.

## `capture` — the Change Data Capture Language

Captures live in `Translate` slices and are the anti-corruption layer: external
shapes in, domain events out. Excerpt; complete document: `references/complete-examples.md`,
"Capture with `source`, `map`, `append` and `children`".

The full capture excerpt (`source`, `key`, `map` with `translate`, two `append` blocks and `children`) is in the reference file.

Read [references/capture-map-and-example.md](references/capture-map-and-example.md) when writing a capture's `map` block, or when you need the excerpt of a capture with `append` and `children`.

**`source <kind>`** takes free-form settings, so a typo is not caught; **`key <property>`** is required to bind (`PLAY0268`); **`append <Event>`** takes at most one `when` clause, and `or` and `and` cannot be mixed in one clause (split into two `append` blocks). Read [references/capture-map-and-example.md](references/capture-map-and-example.md) for source kinds, `key` and specification behavior, the `when` forms table, `children` and `nested`.

## `reaction`

Read [reaction forms](references/reaction-forms.md) when choosing a trigger,
selecting occurrence values or mapping `produces` and `invokes`; complete documents
remain in `references/complete-examples.md`.

A reaction declares **at least one trigger**; everything under a trigger is
optional.

⚠️ **Indentation decides what is a trigger and what is an effect.** `produces`,
`invokes`, `file` and inline code belong **inside** the trigger block, indented
under `when`/`every`/`at`. `description`, `where` and `runs as` sit at reaction level.
Outdenting an effect gives *Expected a trigger in reaction body, got 'invokes …'*
(`PLAY0137`) — the compiler is looking for another trigger where the effect is.

### `where` — narrowing

`where` uses the same condition grammar as `produces … when` and `require`:
`==`, `!=`, the ordering operators, `contains` for a substring anywhere,
`starts with` for one at the beginning, combined with `and`, `or` and parentheses.
These text-condition operators parse but refuse executable binding (PLAY0268);
literal property text rules are a separate v24 contract.

⚠️ **`where` belongs to the reaction, not to one trigger** — it says which
occurrences are worth running for, whatever set them off. A second `where` on one
reaction is an error; combine with `and`/`or` instead.

### Effects — `produces` vs `invokes`

Inline `produces event <Name>` is command-only (`PLAY0474` in a reaction).
Declare reaction-produced events separately and use plain `produces <Name>`.
Do not extend the inline command destination default or its repairs to reactions.

A `produces` without `for` appends to the event source of the event that set the
reaction off. A clock, application or built-in trigger has no such event, so what it
produces needs `for` - a value the trigger carries, or literal text.

`produces` requests an append; `invokes` also runs command gates and may reject.

**Who is the caller of an `invokes`?** Nobody unless `runs as` declares the actor; see *Caller-less invokes and unprotected reads* above before writing one.

Trigger `file`/tagged-code attachments are inventoried, never reference-executed;
see the reaction forms reference for their boundary.

### What it decides from — trigger `reads`

An automation decides from state. A trigger names the views it consults with
`reads <View> [as <alias>] [by <trigger value>]` — the same word and meaning as
a command's `reads`: the state the behavior decides from. Excerpt; complete
document: `references/complete-examples.md`, "Trigger `reads` on an event and on the
clock" (authoring-valid; the `file` bodies stop it binding at ESM v6).

The excerpt of a reaction with `reads` on an event and on the clock is in the reference file.

Read [references/trigger-reads-example.md](references/trigger-reads-example.md) when writing a trigger `reads` with `by` or an alias, or a clock trigger that reads a whole view.

- `reads` goes under a trigger; `by` names a value that trigger takes (else `PLAY0442`); **Clock triggers read whole views.** `by` under them is `PLAY0443`. Read [references/trigger-reads-example.md](references/trigger-reads-example.md) for alias rules (`PLAY0410`–`PLAY0412`, `PLAY0177`), the `@reads` escape (`PLAY0175`, `PLAY0444`) and the reserved `for each`.

⚠️ **Declared, not enforced.** See *Caller-less invokes and unprotected reads* above.

## `trigger`

A `trigger` declares that a name exists and what an occurrence hands the reaction, never what makes it occur. Built-ins are `Startup` and `Shutdown`. An unknown name warns `PLAY0248`; **An application trigger is not a UI event** (`PLAY0326`); under a reaction trigger, values named `description`, `file`, `produces`, `invokes` or `reads` are written with `@`. Read [references/trigger-declaration.md](references/trigger-declaration.md) when declaring or registering a trigger or resolving a trigger name.

## Slice boundaries

Requires Screenplay 4.176.0 or later (`screenplay --version`) for PLAY0694; older compilers leave directionless capture-follow-up co-location unchecked.

> - A capture belongs in `Translate`; a reaction to the local event it appends
>   belongs in a separate `Automation` slice. A directionless Translate containing
>   both warns PLAY0694 (fails `--warnaserror`); explicit inbound refuses it with
>   PLAY0616. A reaction translating a foreign public event can belong in inbound
>   Translate.
> - Keep reaction logic small; anything substantial belongs in a `file` reference
>   where it can be tested on its own.
> - Describe the reaction before you implement it — a document full of `file` lines
>   and nothing else tells a reader nothing.
> - A name that only an integration knows belongs in a `trigger` declaration, so the
>   document says what the reaction is handed rather than leaving the reader to guess.

## Verify

- [ ] `screenplay <model> --warnaserror` reports zero errors and zero warnings
      (standalone, or a `cratis` whose compiler admits ESM v6). Under an older compiler expect
      `PLAY0268` at binding and a false `PLAY0285` on cascade specifications, and report them
      as tool skew.
- [ ] The tool that bound the model is named (standalone, or a `cratis` whose compiler admits ESM v6, for ESM v6 constructs); reference scenarios use
      `screenplay test`/MCP. Unadmitted refusal/redelivery is not executed evidence.
- [ ] Each automation has all four components — occurrence, state consulted,
      conditional logic, resulting command or event. If it always fires
      unconditionally it is co-production, not an automation.
- [ ] Every reaction has a termination condition and cannot loop forever.
- [ ] `produces` is used for facts and `invokes` for intents, never interchangeably.
- [ ] No `when` clause mixes `or` and `and`.
- [ ] Cross-cutting infrastructure is **not** modeled as a `Translate` slice.
- [ ] Every name a reaction takes is carried by the event or trigger.
- [ ] Each trigger `reads` the views its automation decides from; `by` names a
      value the trigger takes, and clock triggers read without `by`.
- [ ] Trigger reads are not protection; protected command reads require opt-in,
      admission and separate target realization. UI clicks are interactions.
- [ ] Every gated invocation declares its trusted actor with reaction `runs as`
      (ESM v10) or records the target gap explicitly; `given caller` is not that actor.

## Route near misses

- Deciding whether a behavior is an automation at all: `cratis-screenplay-event-modeling`.
- Automation, translation and loop-safety method, and the trusted-actor decision: `cratis-screenplay-automations-and-translations`.
- Specifying clocks, triggers, captures and cascades: `cratis-screenplay-specifications`.
- Which tool binds ESM v6 and what each reports: `cratis-screenplay-toolchain`.
- Hand-built code for an automation Stage does not render: `cratis-screenplay-render-and-gap-fill`.
- The commands a reaction invokes: `cratis-screenplay-command-surface`.
- Hand-written C# Chronicle reactors: `cratis-chronicle-reactor`.
