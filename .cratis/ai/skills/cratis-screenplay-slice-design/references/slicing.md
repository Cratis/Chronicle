<!-- cratis-ai-managed: skills/cratis-screenplay-slice-design/references/slicing.md -->
# Slicing, state transitions and the next slice

## Contents

- Slices are the authoring unit
- Declare in story order
- Slice dependencies
- State-transition table (per entity)
- Generic edits
- Rule coverage
- Slice audit
- The next slice
- Handoff line per element
- Rule coverage

## Slices are the authoring unit

In Screenplay every construct already lives in a typed slice, so slicing is not a separate
step at the end: design each behaviour as its slice, then audit the set.

| Slice | Holds | Named after | Default |
|---|---|---|---|
| `StateChange` | command, its events, its constraints, specs | the command (`AssignLocker`) | one command per slice; renderable: required |
| `StateView` | read model, queries, projection/reducer, screen, specs | the view (`LockerBoard`) | one read model per slice |
| `Automation` | reactions, the events they produce | the behaviour (`RemindUncollectedParcels`) | see `cratis-screenplay-automations-and-translations` |
| `Translate` | captures, translated events | the integration (`CarrierManifests`) | see `cratis-screenplay-automations-and-translations` |

Screenplay allows several commands or read models in one slice; prefer splitting unless the
behaviour is genuinely one (and say why in `documentation`).

## Declare in story order

Deciding which slices exist is separate from deciding the order you write them. Write them left
to right as the story happens, one slice at a time: the `StateChange` slice that records a fact,
then the `StateView` slice (read model, projection, query, screen) that the fact feeds, then the
next `StateChange`. Never write all commands, then all views, or any other batch by kind. The
board draws authored declaration and import order, so a batch by kind shows a row of commands
with the views bunched at the end, cut off from the facts they come from, and no slice reads as
one self-contained step of the story. A list grouped by slice type is for counting and review
(`cratis-screenplay-delivery-planning`), never the declaration order.

The compiler checks the result against the presentation timeline. `PLAY0516` (information)
marks a projection, reducer rule or named reaction trigger that uses an event from a slice drawn
to its right, or a `reads` of a view whose builder is drawn later. `PLAY0517` marks a mutually
dependent sibling group that no order can fix. Neither fails `--warnaserror`, so read them after
each batch:

- Move the producer first. MCP and C# workspaces offer a typed repair that moves a sibling or
  pins an import. It changes no executable bytes or identities.
- A view that legitimately reflects a later fact (a waiting list that drops a request once it
  is assigned) may stay where its screen beat is. Say why in the slice `documentation`.
- Feedback reads (a command reading a summary built from facts its own side produces) are
  excluded by the compiler and need nothing.

A folder model needs an ordering root (a sole root, an importing `application.play`, or a unique
importing root). Without one the check is skipped, which does not mean clean
(`cratis-screenplay-toolchain`). The `remove with BerthAssigned` line below is the kept case:
the waiting list legitimately drops a request when the later slice assigns it.

```screenplay expect PLAY0516
domain Harbour.Moorings

concept BerthRequestId : Uuid
concept BoatName : String
concept BerthNumber : String

module Moorings
  feature SeasonalBerths
    slice StateChange RequestBerth
      command RequestBerth
        berthRequestId BerthRequestId identifier
        boatName       BoatName
        produces event BerthRequested
          boatName BoatName = boatName
    slice StateView WaitingList
      readmodel WaitingRequest
        berthRequestId BerthRequestId
        boatName       BoatName
      projection WaitingList => WaitingRequest
        from BerthRequested
          berthRequestId = $eventSourceId
        remove with BerthAssigned
      query WaitingRequests => WaitingRequest[]
      screen WaitingListPage
        data WaitingRequest via query WaitingRequests
        action AssignBerth
    slice StateChange AssignBerth
      command AssignBerth
        berthRequestId BerthRequestId identifier
        berthNumber    BerthNumber
        produces event BerthAssigned
          berthNumber BerthNumber = berthNumber
```

## Slice dependencies

Record, per slice, the events it consumes and which slice produces each. This is for
orientation, not a build order: view specifications use synthetic `given` events, so slices
are built and checked independently. A command or reaction may consult another slice's view
(`reads`; eligible command reads can opt into v25 protection, trigger reads cannot;
see `rule-layers.md` for target gaps). A slice never calls another slice's
command except through `invokes`. Dependencies run in one direction; a cycle through events
means the story is a feedback loop that belongs in an `Automation` slice.

The contract between slices is events: slice B depends on slice A because B's command, rule
or view needs an event A produces. Two other edges are supported and recorded explicitly, never
implied: `reads <View>` from another slice (declare protected-command opt-in and
admission, or mark the legacy/refused shape and target gap, `rule-layers.md`) and `invokes <Command>` from a reaction. No shared state or
other direct call. Name each
slice after its element (the command, the view, the automation, the translation (capture or translator reaction)), not a broader feature name
that spans several. Per slice, record:

```text
Slice:       <CommandName | ViewName | AutomationName>   (StateChange | StateView | Automation | Translate)
Produces:    <events, and when they occur>
Consumes:    <events from other slices, and why this slice needs them>
Upstream:    <slice X: needed because it produces <Event>>
Downstream:  <slice Y: consumes <Event> this slice produces>
Reads:       <views of other slices, with the rule they feed; NOT enforced>
Invokes:     <commands of other slices called by a reaction, and the trusted actor>
```

For `worked-example.md`:

| Slice | Consumes | Produced by | Also reads |
|---|---|---|---|
| InstallLocker | none | | |
| RequestLocker | none | | |
| AssignLocker | none (appends `LockerAssigned`) | | views LockerSummary, RequestStatus (intent, not enforced) |
| LockerBoard | LockerInstalled, LockerAssigned | InstallLocker, AssignLocker | |
| RequestProgress | LockerRequested, LockerAssigned | RequestLocker, AssignLocker | |

### Declare container dependencies

Modules and features may declare `depends on <Name>`, one
target per line. Use a bare sibling name or a qualified module/feature address.
Bare names resolve inside out: siblings, ancestor siblings, then root modules;
qualified names match an unambiguous trailing address. Ambiguity is `PLAY0198`.
Self, ancestor, descendant and unresolved targets warn with `PLAY0554`; repeats
keep the first resolved target and warn with `PLAY0555`.

Requires Screenplay 4.76.0 or later (`screenplay --version`) for `depends on` container dependencies (PLAY0554).

Declarations accumulate on the same owner in ordinal file-path order, independent
of board import order. Layout expansion writes them only in the owner's file.
They are authoring metadata: no execution rule, implementation order, identity
or executable-model revision change.

Once a container declares dependencies, its own inventory must cover counted
explicit references leaving it, including descendants. Parent and child lists
are independent. Module targets cover all their features; feature targets cover
their descendants. `usesFactsFrom`, `reactsTo`, `decidesFrom`, `asks` and `shows`
count; specification-only and outside-context edges do not. `PLAY0552` warns on
uncovered producer modules; `PLAY0553` marks unused declarations and `PLAY0556`
mutual declarations as information. Ambiguous ownership is provisional, not proof
of an undeclared dependency. Inspect MCP `dependency-graph`'s `declarations` view.

Example: Payroll declares Timesheets; its Handover feature declares
Timesheets.Approval and sibling Runs. These are dependency inventories, not
actual references or execution dependencies:

```screenplay expect PLAY0553
module Payroll
  depends on Timesheets
  feature Handover
    depends on Timesheets.Approval
    depends on Runs
  feature Runs
module Timesheets
  feature Approval
```

Source: Screenplay `Documentation/screenplay/slices.md` and `folders.md`.

## State-transition table (per entity)

Rows are states (named after the fact that put the entity there); columns are commands. Each
cell: the resulting event, or the refusal and where it is enforced. Fill every cell; blank
cells hide missing decisions.

For the locker in `worked-example.md`:

| State \ command | InstallLocker | AssignLocker |
|---|---|---|
| (none) | LockerInstalled; refused by UniqueLockerNumber if another locker holds the number | refused: `reads` + `require` (the locker is not free / the request is not waiting) - intent, NOT enforced today |
| installed | refused: LockerInstalledOnce | LockerAssigned; refused by OneLockerPerRequest if the request already has a locker |
| assigned | refused: LockerInstalledOnce | refused: OneRequestPerLocker |

The trap this table caught: `UniqueLockerNumber` alone does not stop the same locker being
installed twice, because an event source may re-claim its own value. The `unique event`
constraint closes that hole. Releasing a locker (a later slice) would add `released by` lines
to both assignment constraints.

Mark how each refusal is enforced: `authorize`, `validate`/`require` (inputs only),
`constraint` (append-time), or **target-enforced** (stored-state; recorded in the slice
`description` and the handoff as NOT enforced today, see `rule-layers.md`). Terminal states have
refusals in every column. Add one line per gated command: the caller fixture that must be
denied (it needs its `then denied` spec).

## Generic edits

A command or event named for a form (`UpdateLocker`, `LockerDetailsUpdated`) records that data
changed, not why. Rules, reactions and history lose their meaning, and the edit path usually
skips rules the install path enforces. Detect it in the state table: a command with the same
result in every row and no refusal anywhere, whose event restates most of the entity.

1. List the values the form can change; for each, ask who changes it and why.
2. Group values that change together for one reason; each group is a command and an event named
   for that reason (`RelocateLocker` -> `LockerRelocated`; `CorrectLockerVolume` ->
   `LockerVolumeCorrected`). Values nobody changes after installation need no command.
3. Give each new command its own state-table column, and fill its cell for every state, with refusals (an assigned locker cannot be
   relocated).
4. Apply rule coverage (below) to each new path.
5. If the business treats a multi-value data-entry correction as one act, keep one correction
   command named for the correction (never `Update`), with the reason in slice
   `documentation`; rule coverage still applies.

A repeated-field event is a review signal; it is major only when business meaning is lost, a
rule is bypassed or consumers become ambiguous. Historical generations repeat prior shape on
purpose and are exempt.

## Rule coverage

For each value that carries a rule, list every path that can set it:

| Value | Rule and layer | Paths that set it | Covered? |
|---|---|---|---|
| locker number | unique (constraint `UniqueLockerNumber`) | InstallLocker -> LockerInstalled; RenumberLocker -> LockerRenumbered | add `unique number on LockerRenumbered` to the same constraint |
| volume | at least one litre (concept `validate`) | InstallLocker; CorrectLockerVolume | covered: the concept travels |
| attendant gate | `authorize` on the feature | every command in the feature | covered: inherited |

A constraint covers another event by repeating its `unique` line under the same constraint name
(the claim is shared) - only when the events set the **same claim**. Command-level
`validate` / `require` and `produces when` branches do not travel: review each path and copy a
rule only where that decision needs it; move a format rule into the concept so it travels.

## Slice audit

- Every event belongs to exactly one `StateChange` (or `Automation`/`Translate`) slice.
- Every command has a reachable, authorized origin.
- Every read model has a consumer and a keyed query when the scope is executable.
- Every slice has a `description` that states its business purpose.
- No slice exists only for technical reasons ("set up storage").

## The next slice

If the request names the capability, build that one. Otherwise, when the current timeline is
fully designed, read the slices in story order and choose, preferring in order:

1. the next stage of an existing lifecycle (a locker can be assigned; can it be released?);
2. an action a screen implies but nothing implements (a button with nothing behind it), or a
   state change with no confirmation view (in the example, the attendant needs a waiting-request view to pick a request);
3. a missing ending, correction or reversal - a correction or reversal names what it corrects
   or reverses (`CorrectLockerVolume`), never `Update<Entity>`;
4. a notification or follow-up the business expects.

If the new slice adds a screen in a model that already composes its UI (templates, layout), give
it the template its sibling screens use (`cratis-screenplay-ui-composition`). A new screen always
has `data` or an `action`, never a placeholder.

Apply this heuristic only when the brief authorizes selecting another capability; when the
requested scope is complete or no supported candidate remains, close the turn without inventing
work (scope ownership: `cratis-screenplay-modeling-lifecycle`). Prefer what continues the
current story over a new thread. When selecting is authorized, not knowing the next capability
is never a reason to stop: a plausible next slice (the next lifecycle stage, an unaddressed
affordance, a missing confirmation or ending) usually exists, and a wrong guess is one slice
that is easy to rename or delete. Posting a question and ending the turn with no slice
written is not an acceptable outcome; flag the assumption and write the slice in the same
turn. Report the capability chosen and why, and the slice type and elements created. A
slice with a command and a view is two slices (`StateChange` and `StateView`): write them one
at a time. In a modeling session, act: write
the slice, note the assumption that chose it, and ask for correction afterwards. A wrong guess
costs one edit; an empty turn costs a round-trip. Record why the slice was chosen in the
handoff.

## Handoff line per element

    `references/verdicts.md`). Hand off with the model root and source revision, the slices
    touched, and **one line per element** (command with origin classified [USER] or [AUTO]
    and refusals, screen with `data`, read model with consumers, slice with events consumed),
    not only problems; a new slice adds why it was chosen. Decided refusals go to
    `cratis-screenplay-scenario-coverage`, never to open questions.

## Rule coverage

- Rule coverage: for each value carrying a rule, list every path that sets it
  (`references/slicing.md`). A constraint on a claim covers every event setting that claim;
  value invariants live in the concept so they travel; command-level conditions are reviewed
  per path, not copied blindly.
