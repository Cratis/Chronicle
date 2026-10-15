<!-- cratis-ai-managed: skills/cratis-screenplay-render-and-gap-fill/references/worked-example.md -->
# Worked example: a refused render, the classification and the report

## Contents

- The model
- The probe
- Classification
- Decision and ledger entry
- The report

A marina model with one renderable command and one snapshot list view. The model compiles and binds;
`cratis render` refuses it, so nothing is published. The example shows how to classify, decide and
report. Probed with the standalone compiler 4.139.0 and cratis 3.53.1 (scratch model, observed); the
earlier 3.28.2 outcome (two `PLAY0268` binding errors) is history.

## The model

```screenplay
// cratis-screenplay-render-and-gap-fill: complete model for the refused-render example.
// Compiles with warnings as errors; `cratis render` 3.53.1 refuses the snapshot list query (STAGE-ESM-010).
domain Harbour.Marina

concept BerthId : Uuid
concept BoatName : String
  validate
    not empty  message "A boat name is required"

policy IsHarbourMaster
  require authenticated and role "HarbourMaster"

module Berths
  description "Which boats hold which berths"
  feature Reservations
    slice StateChange ReserveBerth
      description "A boat reserves one berth"
      command ReserveBerth
        berthId  BerthId identifier
        boatName BoatName
        authorize IsHarbourMaster
        produces event BerthReserved
          boatName BoatName = boatName
      specification ReservingABerth
        given caller
          authenticated
          role "HarbourMaster"
        when ReserveBerth
          berthId  = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          boatName = "Sea Otter"
        then BerthReserved
          for "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
          boatName = "Sea Otter"

    slice StateView BerthRegister
      description "All berths with their current boat"
      readmodel BerthEntry
        berthId  BerthId
        boatName BoatName
      query AllBerths => BerthEntry[]
      projection BerthEntryProjection => BerthEntry
        from BerthReserved
          berthId  = $eventSourceId
          boatName = boatName
```

## The probe

Source identity recorded first (`<commit>+<digest>`), tools named: `screenplay 4.139.0`, `cratis 3.53.1`.
V1 passed; the specification passes in `screenplay test`. Then:

```text
$ cratis render <model-root> --name Marina --destination .ai-work/screenplay/marina/render-probe/Marina -o json
exit 5, nothing published ("planning reported 1 error(s)")
STAGE-ESM-010  Query 'AllBerths' is neither an optional snapshot lookup by identifier nor an
               observable collection query the renderer can preserve.
(+ four PLAY0270 information entries: authoring metadata, not blockers)
```

The list query binds (a list narrowed by nothing, or by one `by` key, is admitted by the binder);
Stage refuses it because it is a snapshot, not an observable collection.

## Classification

| Diagnostic | Class | Why |
| --- | --- | --- |
| `STAGE-ESM-010` `AllBerths` | capability gap | the business question is "show all berths" as a snapshot list; Stage renders a keyed optional snapshot lookup or an **observable** collection (Stage#58 tracks portable query semantics) |

The blocker sits in one slice, `Berths/Reservations/BerthRegister`; `ReserveBerth` renders on its
own: `cratis render ... --scope Berths.Reservations.ReserveBerth` published 46 artifacts to a scratch
destination (observed; the whole model still compiled and bound, and the plain scaffold was added
because the destination had no manifest). Two tempting "model fixes" exist: a keyed `BerthById`
lookup, or marking the list `observable`. The first changes what the harbour master can see; the
second changes delivery from a snapshot to a live subscription. Both are **modeling decisions for
the user**, never renderer workarounds. Here the user keeps the snapshot list.

## Decision and ledger entry

Case A, then case B for `ReserveBerth` (rendered with `--scope`) and case C for the list: the user keeps
the full model, the view is delivered by hand, and the model stays the contract.

```text
F1  scope: Berths/Reservations/BerthRegister (spec: none yet)
    blocking codes: STAGE-ESM-010 x1 (snapshot list query)
    class: capability gap          decision: implementer (user, <date>)
    fallback location: src/Marina.Application/Berths/    spec mapping: none yet (BerthRegister has no specification)
    revisit trigger: Stage#58 (portable query semantics)  last verified: <source identity>
```

Gap noted for the modeler: `BerthRegister` has no specification, so its hand-written delivery
has no oracle for the list; the edit request asks `cratis-screenplay-scenario-coverage` for one
(`given` the reservation event, `then query AllBerths` with the expected rows, subject to
what the reference execution route supports).

## The report

```text
Mode: renderable (user chose)   Source: <commit>+<digest>   tools: screenplay 4.139.0, cratis 3.53.1
V1 authorable:  pass (1 file, screenplay 4.139.0)
V2 diagnostics: not run: no MCP in this session
V3 binding:     pass (cratis validate --executable, bundled 4.137.0)
V4 specs:       pass (screenplay test 4.139.0: 1 executed, 1 passed)
V5 admission:   refused, 1 blocking diagnostic (STAGE-ESM-010), nothing published; scoped to Berths.Reservations.ReserveBerth: not part of this report
V5 publication: not run: admission refused
V5 build:       not run: nothing published
V5 tests:       not run: nothing published
UI omissions:   not applicable (no render)
Hand-off:       F1 to slice-implementer (brief: paths and addresses; contract: the .play slice and its
                specifications; conformance: cratis-application-slice-conformance)
Edit request:   BerthRegister needs a specification for the list (address, expected rows)
Note:           a --scope render of ReserveBerth is a separate V5 result with its own scope line
```

When the list query later renders (for example after Stage ships snapshot lists), probe again with the same inputs. If it does, plan the
migration from hand-written files to managed output with the user before re-rendering; a hand
file on a planned path makes publication refuse.
