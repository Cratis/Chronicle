<!-- cratis-ai-managed: skills/cratis-application-slice-conformance/references/worked-example.md -->
# Worked example: re-delivering a marina berth slice

The contract is this model (complete document). It compiles in design mode: the `ListBerths` list
query blocks binding (V3, `PLAY0268`: a query must declare one caller-supplied `by` argument), so no
specification runs against it here, and the list stays because the domain asks for a list. After delivery one, the model changes: the
harbourmaster wants to retire berths, and the list must stop showing them.

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

    slice StateView BerthList
      readmodel BerthSummary
        berthId   BerthId
        name      BerthName
        maxLength BoatLength
      projection BerthList => BerthSummary
        from BerthRegistered
          berthId = $eventSourceId
      query ListBerths => BerthSummary[]
```

## Delivery one: inventory and reconciliation
```text
RegisterBerth  command  berthId, name, maxLength            code: berthId, name, maxLength, Notes   -> Notes INVENTED
               event    BerthRegistered name, maxLength     code: name, maxLength                   ok
               auth     IsHarbourmaster                     code: authorized                        ok
               spec     RegisteringABerth                   spec class: when_registering/and_berth_is_new   ok
BerthList      property berthId, name, maxLength            code: berthId, name, maxLength          ok
               events   BerthRegistered                     code: BerthRegistered                   ok
               query    list of BerthSummary                code: keyed lookup by id                -> cardinality WRONG
```
Action: remove `Notes` (propose it for the model if the business needs it); restore the list
query. Status after the fix: `done`.

## Delivery two: delta after the model change
The model gains a `RetireBerth` command with event `BerthRetired`, a projection that removes
the berth on that event, and a specification (not shown; the model syntax is in
`cratis-screenplay-model-authoring`). The work list is only what changed:
```text
NEW    command RetireBerth (berthId, authorize IsHarbourmaster)    -> add command
NEW    event BerthRetired                                          -> add event
NEW    spec RetiringABerth                                         -> add spec class, named after it
CHANGE BerthList subscribes to BerthRetired; berth leaves list     -> add subscription, removal spec
```
Then steps 3 and 4 run over the whole slice again; the existing spec `RegisteringABerth` is
unchanged. One old spec fails because the new removal rule is not yet implemented: the code is
fixed, the spec is not edited.

## The report
```text
Contract: .play slice Moorings/Berths (RegisterBerth, RetireBerth, BerthList)
Specification map: RegisteringABerth -> when_registering/and_berth_is_new (pass)
                   RetiringABerth    -> when_retiring/and_berth_exists (pass)
Invented removed: RegisterBerth.Notes
Edit request: none
Status: done
Learnings: slice folders here keep specs beside the slice file
```
If the harbourmaster's note had said "retired berths stay visible in the list" while the
model's specification removed them, the report would instead end `Status: blocked` with the
two readings and an edit request against the `BerthList` slice description.
