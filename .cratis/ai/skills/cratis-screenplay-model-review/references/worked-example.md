<!-- cratis-ai-managed: skills/cratis-screenplay-model-review/references/worked-example.md -->
# Worked example: reviewing a marina berth model

## Contents

- The model under review
- Phase 1: element sweep (working notes)
- Phase 2: entity walk (Berth)
- Phase 3: field lineage
- The report (excerpt)
- After the fix

A small model with one seeded design defect, reviewed in critic mode. The model file is
`berths.play`; line numbers below refer to it. The report follows `report-template.md` and is
shortened: it shows the flagged checks of each phase and elides the passing ones, which a real
report lists in full. The model compiles and, on the standalone 4.139.0 tool, binds and runs one specification (the
`ListBerths` list query binds on Screenplay 4.105.0 or later). The report works in design mode, so it
does not run V3 or V4.

## The model under review

```screenplay
concept BerthId : Uuid
concept BerthName : String
concept BoatLength : Int
policy IsHarbourmaster
  require role "Harbourmaster"

module Moorings
  feature Berths
    slice StateChange RegisterBerth
      description "A harbourmaster adds a berth to the marina."
      command RegisterBerth
        berthId   BerthId identifier
        name      BerthName
        maxLength BoatLength
        authorize IsHarbourmaster
        produces event BerthRegistered
          for berthId
          name      BerthName   = name
          maxLength BoatLength  = maxLength
      specification RegisteringABerth
        given caller
          authenticated
          role "Harbourmaster"
        when RegisterBerth
          berthId   = "9c858901-8a57-4791-81fe-4c455b099bc9"
          name      = "A-12"
          maxLength = 9
        then BerthRegistered
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          name      = "A-12"
          maxLength = 9

    slice StateChange UpdateBerth
      description "A harbourmaster edits a berth."
      command UpdateBerth
        berthId   BerthId identifier
        name      BerthName
        maxLength BoatLength
        authorize IsHarbourmaster
        produces event BerthUpdated
          for berthId
          name      BerthName   = name
          maxLength BoatLength  = maxLength

    slice StateView BerthList
      readmodel BerthSummary
        berthId   BerthId
        name      BerthName
        maxLength BoatLength
      projection BerthList => BerthSummary
        from BerthRegistered
          berthId = $eventSourceId
        from BerthUpdated
          berthId = $eventSourceId
      query ListBerths => BerthSummary[]
```

It compiles with warnings as errors (V1 passes), which says nothing about meaning.

## Phase 1: element sweep (working notes)
```text
S1 RegisterBerth   gate IsHarbourmaster (command); denied spec NONE
S1 UpdateBerth     gate IsHarbourmaster (command); denied spec NONE
S1 ListBerths      gate none (no authorize on query); n/a: nothing to deny
S2 BerthRegistered properties name, maxLength; same as creation event: n/a (it is the creation event)
S2 BerthUpdated    properties name, maxLength; same as creation event: yes
S3 UpdateBerth     form-named: yes
S3 BerthUpdated    form-named: yes (<Noun>Updated)
S5 RegisterBerth   produces 1 (BerthRegistered); state change none -> registered; requires none
S5 UpdateBerth     produces 1 (BerthUpdated); state change none; requires none (non-creating)
S6 BerthRegistered.berthId not in payload (identity comes from `for`): ok
S7 RegisterBerth   StateChange: command, event, specs RegisteringABerth
S7 UpdateBerth     StateChange: command, event, specs no specs
S7 BerthList       StateView: projection, query; specs no specs
```

## Phase 2: entity walk (Berth)
```text
T1 first fact: BerthRegistered, by RegisterBerth
T2 allowed:    UpdateBerth: registered -> BerthUpdated -> registered   (no state needed)
T3 refused:    UpdateBerth before the berth exists: not refused, spec NONE
T4 final:      none declared; can a berth be retired? (business question)
T5 alternative paths: none modeled
```

## Phase 3: field lineage
```text
BerthSummary.berthId    <- $eventSourceId (BerthRegistered, BerthUpdated)       ok
BerthSummary.name       <- BerthRegistered.name, BerthUpdated.name (AutoMap)    ok (silent: counted)
BerthSummary.maxLength  <- BerthRegistered.maxLength, BerthUpdated.maxLength    ok (silent: counted)
BerthRegistered.name    <- RegisterBerth.name                                    ok
BerthUpdated.name       <- UpdateBerth.name                                      ok
BerthUpdated.maxLength  <- UpdateBerth.maxLength                                 ok
RegisterBerth.name      -> BerthRegistered.name                                  ok
Every event has a consumer (BerthList). Terminal reason: none needed.
```

## The report (excerpt)
```text
# Model review: Moorings / Berths
Mode: design   Stance: critic   Review kind: independent
Source identity: 4f1c2ab+3e9a1c0b7d42 (source-identity helper)
Scope: feature Moorings/Berths
V1 authorable: pass (screenplay 4.66.0, 1 file)
V2 executable diagnostics: not run: design mode
V3 binding-ready: not run: design mode
V4 reference specs run: not run: no route
V5 rendered, target-verified: admission not run: design mode; publication, build, tests not run: design mode

## Phase 1: Element sweep (7 checks)
Check S1: gated commands have a denial spec
Status: FINDING #2
Element: Moorings/Berths/StateChange RegisterBerth/command RegisterBerth, Moorings/Berths/StateChange UpdateBerth/command UpdateBerth
Evidence: denied spec NONE for both (berths.play:11, berths.play:35)

Check S2: events do not repeat the creation event
Status: FINDING #1
Element: Moorings/Berths/StateChange UpdateBerth/event BerthUpdated (berths.play:40)
Evidence: properties name, maxLength equal BerthRegistered's

## Anti-patterns identified
MAJOR: Field-copy event, at UpdateBerth/event BerthUpdated
Problem: BerthUpdated records that data changed, not why; no rule can attach to a reason, and
consumers cannot tell a typo fix from a longer limit.
Violates: S2, S3, S5, T3, C7
Fix: replace UpdateBerth with one command per business change (see below)

## Findings
| # | Severity | Kind | Address | Rule | Consequence | Fix |
| 1 | major | defect | Moorings/Berths/StateChange UpdateBerth (berths.play:33), command UpdateBerth, event BerthUpdated | S2, S3, S5 (D) | Root cause. The only way to change a berth resends name and maxLength: the reason is lost, nothing can refuse a change, and no rule or reaction can attach to it. Symptoms: no precondition, no spec, not pinned as a decision | One command and event per business change, for example RaiseBerthLengthLimit and BerthLengthLimitRaised; remove UpdateBerth and BerthUpdated; do not add BerthUpdated to constraints or projections |
| 2 | major | defect | command RegisterBerth (berths.play:11), command UpdateBerth (berths.play:35) | S1, G7 (D) | A boat owner calling either command is not shown to be refused | Add a `then denied` specification with a caller who lacks the role, per command |
| 3 | minor | defect | slice StateView BerthList (berths.play:45) | S7 (D) | The view has no specification; a mapping change goes unnoticed | A view specification from a registered berth |
| 4 | minor | question | event BerthRegistered | T4 | Can a berth be retired or withdrawn from service? | Ask the harbourmaster |

## Edit requests
| 1 | finding 1 | UpdateBerth, BerthUpdated, projection BerthList `from BerthUpdated` | replace by RaiseBerthLengthLimit / BerthLengthLimitRaised | identity-affecting (removal and new event): owning session | no persisted history yet, so removal is safe; confirm first |

## Business questions
1. Harbourmaster, berth changes: when something about a berth changes, is it ever only a
   correction of what was typed, or is it always a real change like a longer pontoon?
   Why it matters: a correction and a real change are different facts.

## Final questions
Q1 yes. Q2 yes. Q3 no (the retirement rule is unknown).

## Verdict
Status: PASS WITH WARNINGS (no critical finding; two major)
Ready to accept (P6): NO - fix finding 1 and finding 2 first
Confidence: medium (design mode; V2 to V5 not run)
```

## After the fix
The same scope after the modeler applied the edit request and added denial specifications.
The review is re-run only on the changed scope. The query contract is unchanged: no edit request asked for a different query, so `ListBerths => BerthSummary[]` stays. This document compiles and binds, and its four specifications pass in `screenplay test`. Replacing the list with a keyed lookup would change the contract and needs its own domain-justified edit request. Stage 4.73.1 would refuse the snapshot list (`STAGE-ESM-010`); only an `observable` list or a keyed lookup renders.

```screenplay
concept BerthId : Uuid
concept BerthName : String
concept BoatLength : Int
policy IsHarbourmaster
  require role "Harbourmaster"

module Moorings
  feature Berths
    slice StateChange RegisterBerth
      description "A harbourmaster adds a berth to the marina."
      command RegisterBerth
        berthId   BerthId identifier
        name      BerthName
        maxLength BoatLength
        authorize IsHarbourmaster
        produces event BerthRegistered
          for berthId
          name      BerthName   = name
          maxLength BoatLength  = maxLength
      specification RegisteringABerth
        given caller
          authenticated
          role "Harbourmaster"
        when RegisterBerth
          berthId   = "9c858901-8a57-4791-81fe-4c455b099bc9"
          name      = "A-12"
          maxLength = 9
        then BerthRegistered
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          name      = "A-12"
          maxLength = 9
      specification RefusingARegistrationFromAnOwner
        given caller
          authenticated
          role "BoatOwner"
        when RegisterBerth
          berthId   = "9c858901-8a57-4791-81fe-4c455b099bc9"
          name      = "A-12"
          maxLength = 9
        then denied

    slice StateChange RaiseBerthLengthLimit
      description "A harbourmaster records that a berth now takes longer boats, for example after the pontoon was extended."
      command RaiseBerthLengthLimit
        berthId      BerthId identifier
        newMaxLength BoatLength
        authorize IsHarbourmaster
        produces event BerthLengthLimitRaised
          for berthId
          maxLength BoatLength = newMaxLength
      specification RaisingTheLimit
        given caller
          authenticated
          role "Harbourmaster"
        given BerthRegistered
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          name      = "A-12"
          maxLength = 9
        when RaiseBerthLengthLimit
          berthId      = "9c858901-8a57-4791-81fe-4c455b099bc9"
          newMaxLength = 12
        then BerthLengthLimitRaised
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          maxLength = 12
      specification RefusingARaiseFromAnOwner
        given caller
          authenticated
          role "BoatOwner"
        when RaiseBerthLengthLimit
          berthId      = "9c858901-8a57-4791-81fe-4c455b099bc9"
          newMaxLength = 12
        then denied

    slice StateView BerthList
      readmodel BerthSummary
        berthId   BerthId
        name      BerthName
        maxLength BoatLength
      projection BerthList => BerthSummary
        from BerthRegistered
          berthId = $eventSourceId
        from BerthLengthLimitRaised
          berthId = $eventSourceId
      query ListBerths => BerthSummary[]
```

The re-review marks finding 1 `fixed` (the event names the reason and carries one property), finding
2 `fixed` for both commands, and leaves finding 3 open: the view still has no specification. It reports no other contract change, because there is none; had the list query been replaced, the re-review would flag it as an unrequested change.
