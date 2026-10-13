<!-- cratis-ai-managed: skills/cratis-screenplay-slice-design/references/field-lineage.md -->
# Field lineage

## Contents

- Where values come from, per element
- The matrix
- The reason line per contributing event
- Gap handling
- Adding or renaming a field along a chain (manual checklist)

Every value in the model has a place it comes from and a place it goes. If you cannot say
where a field comes from, the model is missing an input, an event field or a decision. If an
event field goes nowhere, ask why it is recorded (audit and history are valid answers; say so).

## Where values come from, per element

**Command property**

| Origin | In Screenplay | Notes |
|---|---|---|
| the user types or picks it | screen `action`, form or dialog input | default for business decisions |
| shown before the user acts | form `populate via query` from a read model | the read model must exist and be traced |
| the instance being acted on | the `identifier` property, carried from the screen row or route | |
| allocated by the runtime | `Type generated` on a required scalar generatable Uuid/String concept (implicit UUID is v7; explicit serialized strategy selects v15, Screenplay 4.147.0+ with strict fixtures and whole-source freshness). CLI 3.57.0 / Stage 4.89.1 render supported strategies; types/combinations retain -028/-038; no concept rules. See command-surface `references/generated-values-and-responses.md` | never a request input or form field; no policy, rule or requirement may read it (`PLAY0273`); return it with `returns`; where a tool cannot bind it, executable scope needs explicit allocation |
| an outside party | capture field `$.x` or a reaction trigger value | `cratis-screenplay-automations-and-translations` |
| the caller | not an input: `$context.identity.*` in `produces`; who may act in `authorize`; "acting on own X" is a claim policy matched to the identifier or a command property | never trust a caller-supplied "who am I" |

**Event property** - set only in `produces` mappings, from:
command properties; literals; `$context.occurred`; caller identity (`$context.identity.id|name|userName`).
Those are the portable sources. The event-source id is the `for` value, not a property.

**Read-model property** - set by the projection (PDL) or a reducer, from:
an event field (same-named fields map automatically - AutoMap is on; its matching differs between flat and scoped projections, see `read-model-design.md` "Building it");
`$eventSourceId` (the instance identity); a literal (status words); a counter or arithmetic;
a join on another event stream; children for collections. A reducer is opaque: write its
lineage in slice `documentation`. A read model built by a query `performer` (no events)
has lineage outside the model; say where.

**Screen field** - from the query result named in `data ... via query`. A screen showing a
value that is in no read model is a gap. A value computed for display only (a formatted
duration, a sum of rows) is recorded as `derived: <expression>` with its origin fields traced.

**Conditional inputs.** If one input matters only for some values of another, record the
condition in the matrix, then decide: two commands (two decisions) or one command with an
implication rule. Never leave it implicit.

## The matrix

One row per field, per slice touched, built from the `.play` files, not from memory or an
earlier summary. Report every element, not only problems. Status: `ok`, `gap` (no origin), `unused` (no
destination), `decision` (accepted with a reason).

| Element | Field | Origin | Destination | Status |
|---|---|---|---|---|
| command | | | event field / rule / identity | |
| event | | command prop / literal / context | read-model field / reaction / audit | |
| read model | | event.field / $eventSourceId / literal / derived | screen / automation / query key | |
| screen | | query result field | user | |

Filled for `worked-example.md`:

| Element | Field | Origin | Destination | Status |
|---|---|---|---|---|
| InstallLocker | lockerId | new id for the locker (LockerBoard action) | `for`, LockerSummary.lockerId via `$eventSourceId` | ok |
| InstallLocker | number, volume | user input | LockerInstalled | ok |
| LockerInstalled | number, volume | InstallLocker | LockerSummary; UniqueLockerNumber (number) | ok |
| RequestLocker | requestId | new id for the customer's request (MyRequest action) | `for`; RequestStatus.requestId | ok |
| RequestLocker | preferredVolume | user input | LockerRequested | ok |
| LockerRequested | requestedAt | `$context.occurred` | none yet | decision: kept for queue order (a future waiting-request view) |
| AssignLocker | lockerId | LockerBoard row | `for`; RequestStatus.lockerId via `$eventSourceId` | ok |
| AssignLocker | requestId | attendant picks a request | LockerAssigned.requestId | gap: no waiting-request view to pick from yet (next slice) |
| LockerSummary | assigned | literal on LockerInstalled / LockerAssigned | LockerBoard | ok |
| RequestStatus | state, lockerId | literal / `$eventSourceId` of LockerAssigned (key requestId) | MyRequest | ok |

## The reason line per contributing event

The matrix is the audit; the durable record is StateView slice `documentation`: one line per
contributing event saying which fields it sets and why. Write it as a tagged text block in a
real model (a bare fence warns with `PLAY0397`). `worked-example.md` uses one-string
descriptions only so that it stays a single compiling fence.

````text
slice StateView LockerBoard
  description
    ```text
    The attendant's overview: every locker and whether it is taken.
    LockerInstalled sets number, volume and assigned=false: the locker enters the board.
    LockerAssigned sets assigned=true: the board shows which lockers are taken.
    ```
````

An event with no reason line does not belong in the view; a line you cannot write is a
modeling question. Extend the block whenever a `from` is added.

## Gap handling

- No origin for a command input: who knows this value at the moment of the action? Add the
  read model that shows it, or make it an input, or ask.
- No origin for a read-model field: add the field to the event that should carry it (only if
  that fact really includes it), derive it in the view, or record an open question.
- An event field nobody reads: keep it if the business needs the history; otherwise drop it
  before it is persisted (dropping later is an event-evolution change).

## Adding or renaming a field along a chain (manual checklist)

Until there is a propagation tool (Screenplay#389), walk the chain from consumer back to source and list each
hop before editing:

1. Screen column/field and any `populate via query` mapping.
2. Read-model property and its projection mapping (or reducer).
3. Event property and the `produces` mapping (and `id "OldName"` thinking if the event is
   persisted: renaming an event property is a contract change).
4. Command property, its `validate` rules, and every screen/form/reaction/capture that supplies it.
5. Concept, if the value deserves one.
6. Specifications that set or expect the field.

7. After renaming one side of a projection mapping, check the other: AutoMap matches by name (rules in `read-model-design.md` "Building it")
   An explicit mapping to a renamed-away target fails binding (V3, PLAY0273); a property AutoMap
   used to fill and no longer matches is **not reported** - the field silently stays empty.

Skip a hop and log why when the field already exists there (add) or is not found on that
element (rename); never skip silently. Report the walk hop by hop: element, field, `updated`
or `skipped` with the reason (already present; not on this element); name the chain from
the consuming element back to the source. Run V1 after the chain edit; if a hop cannot be applied, stop
and report that hop. Rename only through the identity-aware path when any hop is persisted
(SKILL.md).

Example hop report (renaming the board column and read-model property `volume` to
`capacityLitres`; the persisted event property stays `volume`, so the projection mapping
becomes `capacityLitres = volume`. Renaming the event property itself would stop at hop 3 and
go to the identity owner):

| Hop | Element | Field | Result |
|---|---|---|---|
| 1 | screen LockerBoard | column volume | updated |
| 2 | projection LockerBoard | `volume = volume` target and read-model property | updated |
| 3 | event LockerInstalled | `volume` | skipped: persisted contract, owner decides |
| 4 | command InstallLocker | `volume` | skipped: not part of this change |
| 5 | concept Litres | none | skipped: unchanged |
| 6 | specifications | read-model and query expectations only; LockerInstalled event expectations stay `volume` | updated |
