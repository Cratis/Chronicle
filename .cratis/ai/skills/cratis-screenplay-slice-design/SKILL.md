---
name: cratis-screenplay-slice-design
description: "Shape Screenplay slices from a discovered timeline: commands with reachable, authorized origins and traced inputs, rules placed by layer, a per-command refusal inventory, events-vs-views decisions, read models per screen component with queries that follow the business view, Level-1 screens, field lineage, state-transition tables, slice dependencies and the next slice. Use when designing slices after discovery, adding a capability or deciding command inputs, rule placement, read-model shape or screen data. Not for: stream boundaries or invariants across streams (use `cratis-screenplay-streams-and-consistency`), reactions and captures (use `cratis-screenplay-automations-and-translations`), which scenarios to write (use `cratis-screenplay-scenario-coverage`), or auditing a finished model (use `cratis-screenplay-model-review`)."
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

Ask the batch below only for what the request leaves open: (1) origin of each action - user, automated, mixed or clock/outside system; (2) which fields matter and what decision each supports; (3) freshness and cost of a stale answer, with each calculated figure confirmed as a view ("does that match your expectation?"); (4) granularity - why each value changes; (5) existing UI or constraints. Read [references/command-inventory.md](references/command-inventory.md) ("Interview batch for slice design") for the impact and follow-up of each.

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
   (`references/field-lineage.md`) and list the values each rule reads: input, state or context
   (`references/command-inventory.md`). A conditional input is a granularity question: two
   decisions are two commands; one decision with a dependent rule is an implication
   (`require a == false or b > c`). Never a boolean attestation input (`confirmsX == true`)
   standing in for a rule. Place each rule in its layer (`references/rule-layers.md`). A rule
   over stored state is written as `reads <View>` + `require ... message "..."` and marked
   **NOT enforced in the model today** in the slice `description`, with its target named (Arc
   `[ProtectedDecision]` + `DecisionRead<T>` (Requires Arc 22.39.0 or later; available to gap-fill
   code with the Arc and Chronicle the Stage scaffold pins); Chronicle DCB; or a constraint). Never copy state
   into the command as input or hide a rule in `handler` / hint prose; descriptions reach code
   only as XML comments. Fill the **refusal inventory**: success, each distinct refusal with
   layer and message, and the denied persona.
4. **Name and shape the event.** Past tense, the specific change that became true
   (`LockerRelocated`, `VolumeCorrected`); never `XUpdated`/`Changed`/`Edited`/`Saved`/
   `Modified`, never `XRecalculated` (a running figure is a view), and no payload restating the
   creation event without a named business change. Naming: `cratis-screenplay-discovery`
   `references/event-naming.md`. Only facts: values the command supplied, literals,
   `$context.occurred` or caller identity. No event-source id in the payload (it is the `for`);
   references to other instances are payload. No nullable property by default (a required value
   or a separate event). No PII on the identifier; one data subject per stream. Add a field only
   when something downstream needs it. Personal values use bare `pii`,
   operational secrets `secret`; declare processing use/basis/retention in `purpose`,
   not a reason. An event-property `subject` may identify a different person from
   the stream but is report-only, not runtime propagation. See command-surface
   `references/compliance.md`.
   Trace caller details to claims or a keyed single-result identity-source query; `$identity.<detail>` reads remain `PLAY0268` (details: `references/field-lineage.md`, "Tracing caller details").

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
   read model needs one unambiguous identity: explicit trailing `key` first. Read `references/read-model-design.md` ("Executable identity of a read model") for the fallback order, the key decision table and the Stage rule.

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
8. **Lineage pass.** After each field edit run `--check field-origins,input-surfaces`. Build the matrix in `references/field-lineage.md` from the `.play` files
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
    `references/verdicts.md`). Hand off with one line per element (command with origin classified [USER] or [AUTO] and refusals, screen `data`, read-model consumers, events consumed), not only problems; decided refusals go to `cratis-screenplay-scenario-coverage`, never to open questions. Full shape: `references/slicing.md` ("Handoff line per element").

## Rules

### Compiler contracts (the tool enforces these)

- At most one `identifier` per command; never on an event property or read-model property (use `key`). At most one builder per read model (`PLAY0191`). Read `references/read-model-design.md` ("Identifier and key contract") for the exact rules.

- A query's `by` goes on its own body line; `from`, single-result filters, scope, performer and composite `by` remain `PLAY0268`; equality filters on collections bind as ESM v14. Read `references/read-model-design.md` ("Query binding") for the binding rules.

- A `unique` constraint names a property the event declares directly; the constraint name is
  its identity (renaming starts an empty index).
- Unauthorized is `then denied`; a broken rule is `then error`; authorization runs first.
- A mapping to an undeclared target fails only at binding (V3, `PLAY0273`); trace unmapped declared properties by hand too (`references/read-model-design.md`, "Mappings to undeclared or unmapped properties").

### Modeling defaults (follow unless the domain gives a reason not to)

- One command per `StateChange` slice (renderable: required, plus no `produces when`).
- One business decision usually produces one event; a decision establishing several facts the
  business recognises separately may produce several (say why in `documentation`).
- Copy an identity into an event payload only when a consumer needs it as a value.
- Keep the one-line summary in `description` (with markers such as **NOT enforced in the model today**); durable reasoning goes in fenced Markdown `documentation`, report-only. Read [model reasoning](references/model-reasoning.md) for owners, syntax, folder merge and PLAY0558/0559.

- Rule coverage: for each value carrying a rule, list every path that sets it (`references/slicing.md`, "Rule coverage"); value invariants live in the concept.

### Review questions

Ask `references/review-questions.md` (reachability, forms in disguise, secrets, fan-in,
collections, invariants, who sees which rows); report a finding only with a domain consequence.

## Public boundaries and view identity

Translate crosses an application boundary (inbound consumes foreign public events; outbound folds private facts into exactly one local public event type); commands never publish public events. Mark view identity with trailing `key`; composite view keys remain unadmitted (`PLAY0268`) - keep the true identity and report the gap. Read [references/public-boundaries-and-keys.md](references/public-boundaries-and-keys.md) when a slice crosses an application boundary or a view needs a composite identity.

## Identity-affecting edits on an existing model

When `.screenplay/identities.json` exists, adding, removing or renaming an addressed element, moving declarations or mapped documents, and any contract change to a persisted event go through MCP, performed by the identity owner (`cratis-screenplay-modeling-lifecycle`); a text edit never gets around a typed-tool refusal. Read [references/identity-edits.md](references/identity-edits.md) for which edits are bounded text edits.

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

## Product sources

Read [references/product-sources.md](references/product-sources.md) when checking a rule against the Screenplay sources and diagnostics (`PLAY0191`, `PLAY0268`, `PLAY0271`).

## Verify

- V1 (V3 if executable/renderable) passes, naming tool and version. Walk the Gate (every element ok / exempt with reason / gap); each persona `Cannot` line has a
  gate plus a denied spec, or is a gap.
- Worked reference: `references/worked-example.md` (design mode, compiles clean). Read when you need a complete design-mode example to compare with command inventories, lineage, and state-transition tables.

## Route near misses

Read [references/neighbouring-skills.md](references/neighbouring-skills.md) when the work is grammar, layout, streams, automations, scenarios or audit rather than slice design.

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
