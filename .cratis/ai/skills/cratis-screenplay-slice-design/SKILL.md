---
name: cratis-screenplay-slice-design
description: "Shape Screenplay slices from a discovered timeline: commands with reachable, authorized origins and traced inputs, rules placed by layer, a per-command refusal inventory, events-vs-views decisions, read models per screen component with queries that follow the business view, Level-1 screens, field lineage, state-transition tables, slice dependencies and the next slice. Use after discovery, when adding a capability or deciding command inputs, rule placement, read-model shape or screen data. Not for: stream boundaries or invariants across streams (use `cratis-screenplay-streams-and-consistency`), reactions and captures (use `cratis-screenplay-automations-and-translations`), which scenarios to write (use `cratis-screenplay-scenario-coverage`), or auditing a finished model (use `cratis-screenplay-model-review`)."
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-slice-design/SKILL.md -->

# Slice design

## Purpose

Turn a timeline of events into designed slices: for each fact, the command that records it and
where its data comes from; for each thing someone needs to see, the read model, query and
screen. Output: compiling `StateChange` and `StateView` slices in the chosen mode, a command
card and refusal inventory per command, a lineage matrix, a state-transition table per entity,
slice dependencies, and the rules the model records but cannot enforce yet.
Grammar: `cratis-screenplay-command-surface`, `cratis-screenplay-read-surface`,
`cratis-screenplay-projections`. Tools, versions and verdicts: `cratis-screenplay-toolchain`.
Lifecycle, modes and hand-offs: `cratis-screenplay-modeling-lifecycle`.

## When / when not

- Use after `cratis-screenplay-discovery`; for a new capability, command inputs, rule placement,
  read-model or query shape, screen data, lineage.
- Not for: which identity a fact belongs to and where invariants are enforced
  (`cratis-screenplay-streams-and-consistency`); reactions, todo lists, captures
  (`cratis-screenplay-automations-and-translations`); which scenarios to write
  (`cratis-screenplay-scenario-coverage`); a full audit (`cratis-screenplay-model-review`).

## Interview phase

Skip if the request already names the command origins (screens, reactions, outside systems),
the decision behind each shown field, and how fresh each view must be. Otherwise ask, in one
batch, only what is missing. Unattended: assume visibly, record each assumption in the phase
report and slice `documentation`, and continue.

1. **Origin of each action.** Is it (A) user-initiated only, (B) automated, (C) a mix, or
   started by a clock or outside system (schedule, webhook, feed)? Impact: separates screen
   commands from automation (and a trusted path); clocks and outside records are designed in
   `cratis-screenplay-automations-and-translations` (trigger, capture, reaction over imports).
   Follow-up: if a mix, which user actions start automation, and what does it decide on its
   own? If outside systems, which one and what data does it send?
2. **Critical fields.** Which data matters most on each screen, and what decision does the
   person make from each field? Impact: fields with no decision behind them are questions, not
   requirements. Follow-up: for a field nobody can justify, ask whether it is history worth
   keeping or can go.
3. **Freshness and cost.** How stale may each view be, and what does a stale answer cost?
   Impact: picks projection versus reducer, and one-shot versus `observable`. Follow-up: if a
   wrong answer is expensive (a booking, a payment), that decision needs a protected read
   (`references/rule-layers.md`), not a view. For every calculated or aggregated figure
   (a total, a count, an average) ask whether it recalculates as source data changes ("so it
   belongs in a view - does that match your expectation?") or whether someone agrees or
   certifies a value (a fact, step 5).
4. **Granularity.** When someone changes these values, why? Impact: separates decisions from
   forms. Follow-up: when a screen edits several values, ask for each who changes it and why.
5. **Existing UI or constraints.** Is there a screen or system to match? Impact: origins and
   screen data. Follow-up: ask to see it; styling belongs to `cratis-screenplay-ui-composition`.

## Procedure

1. **Fix the mode** for the scope (`cratis-screenplay-modeling-lifecycle`): design, executable
   or renderable. It decides which shapes you may use (steps 6, 9). Read the existing model
   before adding to it.
2. **Find each event's origin.** For every event ask who or what makes it happen and through
   which path: a persona's screen `action` or form, a reaction's `invokes`, a capture. The
   origin must be **reachable** (some screen, form, reaction or integration leads to the
   command) and **authorized** (the persona's policies satisfy every gate on module, feature
   and command). A command may have several origins; each must be real. A reaction's `invokes`
   has no caller unless the reaction declares `runs as system role "<Role>"` (ESM v10), so a
   gated command it invokes is refused: declare that trusted path with the role the gate
   admits, never delete the gate.
3. **Design the command** (`references/command-inventory.md`). Imperative business name for
   **one** decision (`ReassignLocker`, `CorrectLockerVolume`). A form verb (`Update`, `Edit`,
   `Save`, `Set`, `Change`, `Manage` + noun) means several decisions share a form: ask which
   separate reasons change these values, then split (`references/slicing.md` *Generic edits*).
   One input per thing the caller decides or supplies; `identifier` on the property naming the
   instance (omit only for allocation); inline `produces event <Name>` (no `for`) when the command owns the event; plain `produces <Name>` needs `for <identifier>`. Trace each input
   (`references/field-lineage.md`). A conditional input is a granularity question: two
   decisions are two commands; one decision with a dependent rule is an implication
   (`require a == false or b > c`). Never a boolean attestation input (`confirmsX == true`)
   standing in for a rule. Place each rule in its layer (`references/rule-layers.md`). A rule
   over stored state is written as `reads <View>` + `require ... message "..."` and marked
   **NOT enforced in the model today** in the slice `description`, with its target named (Arc
   `[ProtectedDecision]` + `DecisionRead<T>` on Arc 22.39.0+ (available to gap-fill
   code with the Arc and Chronicle the Stage 4.73.1 scaffold pins); Chronicle DCB; or a constraint). Never copy state
   into the command as input or hide a rule in `handler` / hint prose; descriptions reach code
   only as XML comments (Stage#178). Fill the **refusal inventory**: success, each distinct refusal with
   layer and message, and the denied persona.
4. **Name and shape the event.** Past tense, the specific change that became true
   (`LockerRelocated`, `VolumeCorrected`); never `XUpdated`/`Changed`/`Edited`/`Saved`/
   `Modified`, never `XRecalculated` (a running figure is a view), and no payload restating the
   creation event without a named business change. Naming: `cratis-screenplay-discovery`
   `references/event-naming.md`. Only facts: values the command supplied, literals,
   `$context.occurred` or caller identity. No event-source id in the payload (it is the `for`);
   references to other instances are payload. No nullable property by default (a required value
   or a separate event). No PII on the identifier; one data subject per stream. Add a field only
   when something downstream needs it. At 4.127.0 personal values use bare `pii`,
   operational secrets `secret`; declare processing use/basis/retention in `purpose`,
   not a reason. An event-property `subject` may identify a different person from
   the stream but is report-only, not runtime propagation. See command-surface
   `references/compliance.md`.
   Trace caller details to claims or a keyed single-result identity-source query
   (built-in/claim/literal key, detail-independent effective gate, compatible result).
   The `identity` block is authoring metadata; executable `$identity.<detail>` reads
   remain PLAY0268. `scoped to` is opaque, not a detail reference; see command-surface
   `references/context.md`.
5. **Events or views?** For each value someone needs: was it decided or supplied (event), or
   can it be worked out from facts already recorded (view)? Test, anti-patterns and where the
   outputs of an automation go: `references/read-model-design.md` *Events or views*. Decided calculations (a quoted
   price, a set fee) are facts. Running figures are views. Time-relative states ("overdue")
   are not stored: store the deadline.
6. **Design the read side** (`references/read-model-design.md`). Enumerate every screen and
   automation first: one read model per screen component or automation need; the typical
   slice is `read model -> screen -> command -> event`. Pick the builder (declarative projection first, reducer when
   guards cannot be expressed). The query follows the business view: a list where people scan a
   list, a keyed lookup where they open one thing. For **executable or renderable scope**, every
   read model needs one unambiguous identity: explicit trailing `key` first, else
   single-instance queries agreeing on `by xId XId`, else conventional `*Id` inference
   only with an admitted list query and no single-instance identifier. Queryless views
   need explicit `key`; read-surface `references/named-keys.md` gives the exact fallback.
   The identifier equals the projection's effective key (decision table in
   `references/read-model-design.md`; Stage rule in `references/admission.md` of
   `cratis-stage-rendering-and-sandbox`). Collections and observable delivery bind at
   4.135.0; check renderer admission separately.
   Whose rows is part of the view; a caller-supplied filter is not access control.
7. **Screens, Level 1.** `data <RM> via query <Q>` plus `action <Command>` per thing the user
   can do there. Build each slice as read model, screen, command, event. A screen acting on an
   existing instance also shows `data` from a view that supplies the identifier and the state
   the user decides on; only a creation screen may lack `data` (say so in the slice
   `description`). A screen with neither `data` nor `action` is a gap. A screen with several
   components has one `data` line per component, each from its own read model
   (`references/read-model-design.md`). One actor's point of view
   per screen, at a real beat of the story, not one per persona for symmetry; know where the
   actor sees each state change's result. Beyond Level 1: `cratis-screenplay-ui-composition`.
8. **Lineage pass.** Build the matrix in `references/field-lineage.md` from the `.play` files
   (read them), not from memory or an earlier summary. For each read model, write one line per
   contributing event in its StateView slice `documentation`: which fields it sets and why. An
   event with no reason does not belong in the view. A field without an origin is a gap: add
   the missing input or event field, or record an open question. Never invent a source.
9. **Slice and continue** (`references/slicing.md`): state-transition table per entity; one
   command per `StateChange` slice by default (required for renderable scope), one view per
   `StateView` slice; record the events each slice consumes and which slice produces them. If
   the request names the next capability, build that; if the brief authorizes selecting one, use the heuristic there and state why (Not knowing the next capability is never a reason to stop); scope complete or no candidate left: close the turn.
10. **Validate** after the batch: V1 with warnings as errors, then V3 when the mode is
    executable or renderable, with the tool and version named (`cratis-screenplay-toolchain`
    `references/verdicts.md`). Hand off with the model root and source revision, the slices
    touched, and **one line per element** (command with origin classified [USER] or [AUTO]
    and refusals, screen with `data`, read model with consumers, slice with events consumed),
    not only problems; a new slice adds why it was chosen. Decided refusals go to
    `cratis-screenplay-scenario-coverage`, never to open questions.

## Rules

### Compiler contracts (the tool enforces these)

- At most one `identifier` per command; never on an event property; never on a read-model
  property (use `key`; otherwise identity comes from single-instance `by`, or
  conventional `*Id` only with an admitted list query and no single-instance identifier).
  Queryless read models need explicit `key`.
- At most one builder (projection or reducer) per read model (`PLAY0191`).
- A query's `by` goes on its own body line. Optional keyed and collection queries
  bind, including observable delivery. Caller-supplied equality filters on collections
  bind as ESM v14 since 4.143.0; `by` ANDs with supplied filters. `from`, single-result
  filters, scope, performer and composite `by` remain PLAY0268 for their respective
  context/cardinality/opaque/multipart gaps. Read read-surface
  [equality filters](../cratis-screenplay-read-surface/references/equality-filters.md).
- A `unique` constraint names a property the event declares directly; the constraint name is
  its identity (renaming starts an empty index).
- Unauthorized is `then denied`; a broken rule is `then error`; authorization runs first.
- A mapping to an undeclared target fails only at binding (V3, PLAY0273); a declared property that nothing maps (for example after a one-sided rename that AutoMap relied on) is reported by no tool: check field lineage by hand.

### Modeling defaults (follow unless the domain gives a reason not to)

- One command per `StateChange` slice (renderable: required, plus no `produces when`).
- One business decision usually produces one event; a decision establishing several facts the
  business recognises separately may produce several (say why in `documentation`).
- Copy an identity into an event payload only when a consumer needs it as a value.
- Keep the one-line summary in `description`, including short status markers such as
  **NOT enforced in the model today** (with its target) or **ASSUMED**; record durable
  reasoning, assumptions, causes and rejected alternatives in fenced Markdown
  `documentation` on the module, feature, slice, command, event, read model or reaction, not `//` comments or disconnected notes. These are report-only, not
  enforcement. Read [model reasoning](references/model-reasoning.md) for owners,
  syntax, folder merge and PLAY0558/0559.
- Rule coverage: for each value carrying a rule, list every path that sets it
  (`references/slicing.md`). A constraint on a claim covers every event setting that claim;
  value invariants live in the concept so they travel; command-level conditions are reviewed
  per path, not copied blindly.

### Review questions

Ask `references/review-questions.md` (reachability, forms in disguise, secrets, fan-in,
collections, invariants, who sees which rows); report a finding only with a domain consequence.

## Public boundaries and view identity

Translate crosses an application boundary: inbound consumes foreign public events
or data and produces private facts; outbound folds private facts into exactly one
local public event type. Commands never publish public events. Put direction in
the slice outline and trace every exported field; read automations-and-translations
`references/public-events-and-direction.md` for the released ESM v9 contract.

Mark view identity with trailing `key`. Use a composite key for independent view
coordinates (resource + period), a natural single id for one entity's state.
Supply every named part in single-instance queries and command/trigger reads;
see read-surface `references/named-keys.md`. Composite view keys remain
unadmitted at 4.135.0 (PLAY0268, #599), unlike admitted composite stream routes.
Keep the true identity and report the gap rather than inventing a concatenated id.

## Identity-affecting edits on an existing model

Classify an edit by whether it changes catalog addresses. When `.screenplay/identities.json`
exists, adding, removing or renaming an addressed element (declarations, properties of commands, events,
read models, composite types and triggers, queries, query arguments, specifications), moving
declarations between files, creating, deleting, renaming or moving a mapped `.play` document, and any
contract change to a persisted event go through MCP, performed by the identity owner
(`cratis-screenplay-modeling-lifecycle`, *Classify by catalog address*; evolution classes:
`cratis-screenplay-streams-and-consistency`). Bounded text edits are only for edits that
preserve addresses (descriptions, rule and expression bodies, mappings between existing
members). Without `identities.json`, plain text edits are fine. Never use a text edit to get
around a refusal from the typed tools.

## Gate

Slice design is done when these hold; otherwise report what is open.

- [ ] Every command: origin reachable and authorized; every input traced; rules placed by
      layer; stored-state rules recorded NOT enforced with a named target; no form-verb name.
- [ ] Every command has a refusal inventory (success, each refusal with layer and message,
      denied persona); every gated command and query has a `then denied` row.
- [ ] Every event is past tense, a specific change, and consumed or justified.
- [ ] Every read model has a consumer and, for executable scope, one identity: explicit
      `key`, else agreeing single-instance `by`, else unambiguous `*Id` only with an
      admitted list query and no single-instance identifier. Queryless views use `key`.
- [ ] Every StateView's `documentation` gives a reason per contributing event; every read-model
      field and screen field is traced; every `from` event sets a field.
- [ ] No `screen` without `data` or `action`; every acting screen has `data` or a stated
      exemption.
- [ ] State-transition table per entity, including refused transitions; rule coverage listed
      per rule-carrying value; slice dependencies recorded.
- [ ] V1 passed, and V3 (executable/renderable scope) with expected codes only, tool and version named.

## Verified product sources

| Source | Pin | Used for |
| --- | --- | --- |
| Screenplay | `v4.66.0` (`c89198b`) | `commands.md`, `constraints.md`, `specifications.md`, `diagnostics.md` (`PLAY0191`, `PLAY0268`, `PLAY0271`, `PLAY0350`, `PLAY0381`, `PLAY0397`), `projections/keys.md` |

Examples compile with `screenplay` 4.68.0 `--warnaserror` and `cratis screenplay validate
--warnings-as-errors` (cratis 3.28.2 or later); other pins: `cratis-screenplay-toolchain`
`references/versions.md`. Compiling proves syntax, not that the model is executable or renders.

## Verify

- V1 (V3 if executable/renderable) passes, naming tool and version. Walk the Gate (every element ok / exempt with reason / gap); each persona `Cannot` line has a
  gate plus a denied spec, or is a gap.
- Worked reference: `references/worked-example.md` (design mode, compiles clean). Read when you need a complete design-mode example to compare with command inventories, lineage, and state-transition tables.

## Route near misses

- Grammar: `cratis-screenplay-command-surface`, `cratis-screenplay-read-surface`,
  `cratis-screenplay-projections`; layout and forms: `cratis-screenplay-ui-composition`.
- Streams and invariants: `cratis-screenplay-streams-and-consistency`; automations and captures:
  `cratis-screenplay-automations-and-translations`.
- Scenarios and specification forms: `cratis-screenplay-scenario-coverage`,
  `cratis-screenplay-specifications`; audit: `cratis-screenplay-model-review`.

## References (load on demand)

- `references/command-inventory.md` - command card, interview questions, refusal inventory.
- `references/review-questions.md` - design review questions.
- `references/rule-layers.md` - rule placement, executable authorization, PII.
- `references/derived-condition-events.md` - read when a computed condition might deserve its own event: the test, who produces it, which causes to fold.
- `references/field-lineage.md` - origins per element, matrix, reason lines, chain edits.
- `references/read-model-design.md` - components, fan-in, list vs keyed, singletons, scope.
- `references/slicing.md` - slice naming, dependencies, state table, generic edits, coverage,
  next slice.
- `references/worked-example.md` - parcel lockers, design mode, complete and compiling.

## Lineage

Attribution: `references/provenance.md`. Read when checking source attribution or adaptation history.
