<!-- cratis-ai-managed: skills/cratis-chronicle-read-model-specifications/references/view-lifecycle-checks.md -->
# View lifecycle checks

## Contents

- How to use this list
- Population
- Update
- Removal
- Empty and several-instance cases
- Closing checklist

Step 6 of `SKILL.md` gives the coverage inventory. This reference expands its three lifecycle
questions (does the first event populate, does a later event update, does an end event remove)
into checks you can tick per read model. Use it for each StateView slice you deliver. Adapted from
Martin Dilger and Nebulit GmbH's build-state-view (see `provenance.md`), with the SQL operations,
migrations and projection specification harness replaced by `ReadModelScenario<TReadModel>`.

Take the contract first (the `.play` read model, its projection and its specifications, or the
agreed outline). Write one specification per contract specification, using its example values, and
add the cases below only where the contract states the behavior; an uncontracted rule found in the
projection is a proposal for the contract, not silent coverage.

## How to use this list
For each read model, list its properties and its events in a small table (property, events that
set it, events that clear it). Walk the three lifecycle stages against it. A cell with no
specification is a gap to fix or to report.

## Population
The first event creates the instance with the properties it sets.
- Seed only that event (`Given.ForEventSource(id).Events(first)`) and assert each property it sets.
- Assert the key: the instance is the one for that event source (or for the contract's key
  property). Check the key resolution before the property mapping when `Instance` is `null`.
- Assert a property the event does not set stays at its default; do not assert a value the
  contract does not state.
- Cover every event that can create the instance if the contract has more than one.

## Update
A later event changes the instance.
- Seed the first event, then the update event, in that order, and assert the changed property
  has its new value.
- Assert every property the update event does **not** set is unchanged. A partial update that
  clears or overwrites other fields is the common defect; AutoMap can wire an identically named
  property of another event over an explicit setter, so look at the property the event was not
  meant to touch.
- Cover each event that updates the view, with the contract's example value for each.
- Where several events set the same property, specify the order that matters and the last-writer
  outcome the contract states.
- For a child collection, assert the child is added, updated in place, and that siblings stay
  intact.

## Removal
An end event removes the instance, or clears a property or a child.
- Seed the creating event and the removing event, then assert absence: `Instance` is `null`, or
  the child is gone from the collection. Absence is a result, not a skipped case.
- Assert a view that must keep ended items does not remove them but sets the contract's status
  value, and that the removal event does not delete other instances.
- A suite where every specification only adds is green and untested for removal; add the end
  event for each removal in the contract.

## Empty and several-instance cases
- **Empty.** With no matching events `Instance` is `null` (the empty initial state, or the
  baseline you passed to the scenario). Assert it where "no result" is a behavior the contract
  states, such as a list with no rows.
- **Several instances.** Where a view holds one row per source, seed two event sources and use
  `InstanceForEventSourceId(id)` or `Instances`; a single `Instance` would throw
  `MultipleInstancesMaterialized`. Assert that one source's events do not touch the other's row.
- **Subscribed events.** The events your specifications seed, the projection's subscriptions and
  the contract's events agree in both directions; report any difference to
  `cratis-application-slice-conformance`.

## Closing checklist
- [ ] Every read-model property is asserted in at least one specification
- [ ] Every creating event has a population specification asserting the properties it sets and the key
- [ ] Every updating event has a specification, and the properties it does not set are asserted unchanged
- [ ] Every removal or clearing event has a specification asserting absence
- [ ] The empty case and, where the view holds many rows, a two-source case are specified when the contract states them
- [ ] Example values and relationships come from the contract; fresh identifiers only where isolation needs them
- [ ] No property, column or event beyond the contract's read model appears in the code or the specifications
- [ ] Each new specification was seen to fail once (drop a seeded event, confirm the assertion goes red)
