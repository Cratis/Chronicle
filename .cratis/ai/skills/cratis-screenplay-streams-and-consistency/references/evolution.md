<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/evolution.md -->
# Event evolution

## Contents

- Classify the change
- Stored event names
- Enum identities are stored contracts
- Compatibility scenarios (write before editing)
- Identity rules
- Identity owner
- Generations, pins and storedName diagnostics
- Enum member numbers

Stored facts outlive the code and the model that wrote them. Plan a change by asking what
happens to facts already stored, to every reader of the event, and to the deployment window
when old and new code run side by side. The best evolution is the one you avoid: short
lifecycles and events that record what the business decided (not table rows) change less.

## Classify the change

**Release baseline:** commit `.screenplay/release-baseline.json`,
written by explicit `screenplay baseline admit <root> --release <label>` in the release
job; gate later edits with `screenplay baseline check <root>`. Without a baseline,
current-property edits still require exactly one of `newGeneration` or
`eventNeverPersisted`; unadmitted revisions default to in-place edits; admitted
revisions accept only explicit `newGeneration`. Never hand-edit the sidecar except
a reviewed correction. The baseline freezes resolved shared schemas too, not just
event declaration text. Full CLI exits, MCP review and source evidence:
model-authoring [release baseline](../../cratis-screenplay-model-authoring/references/release-baseline.md).

Edit class depends on whether the event is **persisted** (facts may be stored: the model was
accepted, deployed or rendered into a running system; ask when unsure). Changing an
unpersisted event's members is an ordinary text edit when no `.screenplay/identities.json` exists; with it, adding or removing event properties changes catalog addresses and goes through MCP. Any change to a
persisted event's contract (new generation, property added, removed or retyped, meaning) is
**contract evolution**: identity-affecting (identity procedure in `cratis-screenplay-modeling-lifecycle`). Write the
compatibility scenarios below first; the identity owner then applies the change (see
*Identity owner*). The table assumes a persisted event.

| Change | Old facts still mean the same? | Screenplay action | Edit class |
|---|---|---|---|
| add a property with a business-defined value for old facts | yes | new `generation N+1`, every generation declared in full in the same slice; record the default and its reason for old facts in slice `documentation` | contract evolution |
| add a property with no sensible old value | the old fact lacks information | usually a **new event** for the new situation, or a separate fact recording the extra information | review with the expert |
| remove a property | yes, if no reader needs it | new generation without it; every reference fails with PLAY0273 until removed | contract evolution, check readers |
| change meaning (same name, different business fact) | **no** | **new event** with its own name; old event stays for history | identity: add, not rename |
| rename (same meaning) | yes | identity-aware rename by the identity owner; `id "OldName"` carries the old stored name in ESM v16; the target must admit it explicitly | identity-affecting |
| split one event into two / merge two | depends | new events from now on; never rewrite history; projections read both old and new | identity-affecting |
| rename a constraint | n/a | starts an empty index: values claimed before are unprotected. Ask first | identity-affecting |

## Stored event names

A rename that keeps the same business fact and must read old stored events pins
its old stored name: `event NewName` with `id "OldName"`. Screenplay
carries a differing pin as `storedName` on the current event contract and selects
ESM v16. All generations share that effective stored name (pin, otherwise name);
it does not replace or rehash the catalog's portable `EventContractId`.

Pins must be event-name identifiers: ASCII letter or underscore first, then word
characters, nonempty and without surrounding whitespace. Invalid pins, different
effective names across generations and duplicate stored names across the application
refuse binding with **PLAY0472**. An equal-name pin is a no-op: no `storedName`, no
v16 selection; PLAY0471 removal preserves ESM bytes. Models without differing pins
retain earlier versions, bytes and revisions. MCP's
compatibility `eventPinWarnings` array is empty.

Consumers must explicitly admit v16 and preserve `storedName`, or refuse it.
A renderer that stops before v16 does not: it emits argument-free `[EventType]`, and a CLI
bundle can carry an older compiler. Do not
promise rendered storage compatibility or remove the pin to make rendering pass.
A passing reference specification is not a stored-history test: reference facts
still use contract/display names. If an older renderer already stored events under
the new name, switching the stored type needs a reviewed baseline correction or
data migration, not a silent re-render. The `--from-esm` stored-name behavior is in the
release-baseline reference.

Requires Screenplay 4.152.0 or later (`screenplay --version`) for `storedName` (ESM v16) and 4.153.0 or later for the release baseline.

Sources: Cratis/Screenplay `Documentation/screenplay/events.md`,
`Source/DotNET/Screenplay/Semantics/SemanticModelBinder.StoredEventNames.cs`,
`SemanticModelBinder.Structure.cs` in the same folder, and
`Source/DotNET/Screenplay.Mcp/McpWorkspaces.Refactoring.cs`.
Target evidence: toolchain `references/diagnostics.md` (Reserved stored names and event pins).
Source/stream stored names already travel in ESM v8; stream `All` remains reserved
regardless of a pin (toolchain `sources-and-streams.md`).

A test for "generation or new event": a new version must be convertible from the old one
without inventing facts. If it is not, it is a different event.

## Enum identities are stored contracts

Unnumbered enums use zero-based declaration positions: a reorder or insertion can
reinterpret stored numbers. Before reordering or removing members, pin each to its
**existing stored number**, not its new position. `propose-number-enum-members`
pins an unnumbered enum to current positions; use it before changing order, only
when those positions match storage. Never renumber or reuse a published member's
number. Removing a member still needs replay and mixed-reader compatibility decisions.

Numbered members may have sparse or negative unique int32 values, all or none per
enum. Source authoring shipped; non-positional numbers bind as ESM v12
Positional pins retain older executable bytes/version. A renderer that admits v12 renders
explicit member numbers; keep the pins.
Short-lived input/view-only enums may remain positional. Use opt-in
`--check enum-pinning` (PLAY0671) to find unnumbered enums used by event properties.
Syntax/example and source evidence: command-surface [enum numbers](../../cratis-screenplay-command-surface/references/enum-numbers.md).

## Compatibility scenarios (write before editing)
For each changed event, answer in the slice `documentation` or `STATE.md`:
1. **Replay**: does every projection still build from generation 1 facts? Which value do old
   facts give the new property (default, absent, derived)?
2. **Readers**: which projections, reactions, constraints, specifications and other modules
   (imports) reference this event? Each needs a decision.
3. **Mixed deployment**: can old code read new facts during rollout? If not, order the rollout
   (readers first) or use a new event.
4. **Constraints**: does a constrained property change type, casing or meaning? A changed
   meaning under the same constraint name corrupts uniqueness.
5. **Migration**: Screenplay does not express migrations or upcasters. Chronicle's route is
   event-type migrations between consecutive generations (defaults, renames, split/combine of
   properties, value maps); a migration maps one event to one event and never splits a fact into
   several. A target without migration support must reject an evolved contract. Record who
   writes the migration and test replay of old-generation facts; disposable greenfield data
   needs none.
6. **Specifications**: `given` facts use the current shape only (a historical shape is
   PLAY0449). Old-shape behaviour is checked in the target, not in `.play` specs.

## Identity rules
- Generations of one event live in one slice; a same-named event in another slice is a
  different contract, not a generation (and ambiguous to the binder).
- Leave `id` absent on new events; a pin equal to the event's current name is information
  PLAY0471 (Screenplay `events.md`). Without a release baseline, renames pin by default;
  "never persisted" requires storage evidence. With a baseline, an unadmitted event gets
  no new pin; any admitted revision requires retaining its stored name and refuses
  `eventNeverPersisted`. Inspect `eventRename.mutability` / `pinAdded` before apply.
- Every rename, move or removal of an event, constraint, command, read model, slice, feature
  or module is identity-affecting and is done by the identity owner.

## Identity owner
Procedure and ownership: the identity procedure in `cratis-screenplay-modeling-lifecycle` (the
owning session, or a brief that asks for the rename, applies it; a subagent without MCP returns
the catalog-changing request to the owner; never leave a requested change silently unapplied).
- Several generations select ESM v4; that binds executably on both tools (compiled
  with `evolution-example.md`). Stage (still) admits v4 but refuses any evolved event and its
  dependent scope with `STAGE-ESM-026` (it cannot render event-type migrations yet, Stage#204,
  which depends on Screenplay#71), and its runtime still refuses evolved events: the evolved
  events are gap-fill, with migrations hand-written per `cratis-chronicle-event-type-migration`.

Complete compiled example: `evolution-example.md` (generation, rename pin, meaning change).

## Generations, pins and storedName diagnostics

- Generations are declared in full, numbered 1..N without gaps, in the same slice
  (PLAY0446-0448). Referencing a removed property is PLAY0273; a `given` needing a previous-generation
  shape is PLAY0449. An `id` pin equal to the event's current name is PLAY0471 (information).
  Differing pins carry `storedName` in ESM v16;
  equal-name pins are no-ops. Pins are event-name identifiers, shared by all
  generations and unique application-wide (PLAY0472). Consumers must admit v16;
  a renderer that stops before v16 does not. See `references/evolution.md`.

## Enum member numbers

   Pin persisted/public enum member numbers before reordering or removing
   members; never renumber or reuse a published number. Positional pins preserve
   executable bytes; non-positional pins need ESM v12 consumer admission.
