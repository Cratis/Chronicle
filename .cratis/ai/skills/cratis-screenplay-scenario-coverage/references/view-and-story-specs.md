<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/view-and-story-specs.md -->
# View specifications and lifecycle families

## Contents

- Per read model, decide before drafting
- Required specs per projected read model
- Lifecycle families (storyline emulation)
- Worked family: a todo list
- Removal is a positive obligation
- Ordering, lag and rebuild
- Performer-backed views (no events build them)
- Cross-context views
- Gated views

Views cannot reject a stored fact, so view specs have no error cases. They answer: after these
facts, what does the screen (or automation input) show?

## Per read model, decide before drafting
Write one line per read model in the coverage matrix:
`<ReadModel>: family yes|no - <reason>`.
- **Family yes** when several facts, or the same fact repeated with different values, walk one
  row through states worth narrating (reserved -> departed; a queue item opened then closed;
  a running total).
- **Family no** when the view is filled by one fact and nothing changes it later.
Deciding all read models the same way in one batch usually means the decision was skipped.

## Required specs per projected read model
| Situation | Spec |
|---|---|
| Population | `given <Event>` (with `for`) -> `then query <ById>` with `arguments` and `result` |
| Each update | earlier facts as `given`, the updating fact as `when append` -> `then query` |
| Removal | `when append <RemovingEvent>` -> `then no readmodel <RM> for "<key>"` (not renderable) |
| Absence | a fact that must NOT create a row -> `then query` with `arguments` and no `result` |
| Join / child | the joined or child fact arriving -> the affected row only |
Use `then readmodel` when the point is the stored state; use `then query` when the point is
what a caller can ask for. A read-model assertion is not a query assertion.

## Lifecycle families (storyline emulation)
Screenplay has no multi-step storyline today (multi-step specifications with checkpoints are only an idea: Screenplay#394, open, not available) and a spec has at most one action. Emulate one with
independent specs that share a cast and carry cumulative givens:
- Sketch the business paths first, forks included: reserved -> departed, reserved -> cancelled.
  Do not flatten mutually exclusive paths into one apparent sequence.
- Names `<View>Lifecycle1<State>`, `<View>Lifecycle2<State>`, ... Each member repeats the facts
  of its own path as `given`, adds one step (`when append` or the step's events) and asserts the
  state. No member depends on another having run.
- `BerthBoardLifecycle2Departed` and `...3Cancelled` in `berth-reservations.md` are sibling
  branches after `...1Reserved`; the numbers are labels, not an execution order.
- A family may return to an earlier state (available -> reserved -> available again): assert the
  earlier value again, do not assume it.
- Members must agree with each other and with the view's single-transition specs. If a single
  spec asserts `status = "reserved"` after a departure, one of them is wrong - fix the model or
  the spec, never both silently. A single-transition spec can satisfy a family obligation; link
  it rather than copying it.
- A command never gets a family. Commands get one spec per transition.

## Worked family: a todo list
A read model whose rows appear when work is opened and disappear when it is done is the prime
candidate for a family. Walk one row through three states (`OpenInvoices`: empty, one entry after
`InvoiceIssued`, empty again after `PaymentReceived`) as three independent specs, each with its
own full givens. The excerpt is copied from `invoicing-dues-example.md`; the last spec asserts
both that the stored row is gone and that the keyed query returns nothing. The whole-list query
(no key) is executable too (Screenplay 4.105.0 or later): `invoicing-dues-example.md` adds
`ListOpenInvoices => OpenInvoice[]` with one spec that expects one `result` row and one that
expects the empty list after `PaymentReceived`. Only `filter`, `scoped to` and `performer` queries stay design-mode.
The source document also asserts the other terminal outcomes (`InvoiceVoided`, `InvoiceSettled`) the same way; they are not copied here.
```screenplay excerpt parent=references/invoicing-dues-example.md#1
specification OpenInvoicesLifecycle1Empty
  given caller
    authenticated
    role "Accounts"
  then query OpenInvoiceById
    arguments
      invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"

specification OpenInvoicesLifecycle2Issued
  given caller
    authenticated
    role "Accounts"
  given InvoiceIssued
    for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
    member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
    amount = 130
  then query OpenInvoiceById
    arguments
      invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
    result
      invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
      member    = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
      amount    = 130

specification OpenInvoicesLifecycle3Paid
  given caller
    authenticated
    role "Accounts"
  given InvoiceIssued
    for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
    member = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
    amount = 130
  when append PaymentReceived
    for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
  then no readmodel OpenInvoice for "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
  then query OpenInvoiceById
    arguments
      invoiceId = "9c1f0a52-6a4e-4b1c-9f55-0d2c7b8e1a01"
```
One walkthrough would assert the lifecycle; three specs assert each transition and, with the
same cast, read as one. Where a view also has single-transition specs, read the family first: a
single spec that repeats a state the family already asserts is redundant (link, do not copy), and
one that contradicts it is wrong.

## Removal is a positive obligation
`remove with` existing is not coverage. Establish the row with its real source events, append
the removing event, assert `then no readmodel <RM> for "<key>"`. Where a wrong key could remove
unrelated data, add a second instance and assert it survives (`RemovingOnlyTheCancelledBooking`).
For child removal or clearing a nested object, assert the parent's resulting collection or
object, not root absence.

Keep three claims apart:
- one stored instance is absent: `then no readmodel`;
- a query returns no rows: `then query` without `result`, or `when query` + `then no result`;
- a list-valued property is empty: assert that property as `[]`.
List-query results use repeated `result` blocks; count is exact and rows match
by read model and instance key regardless of order. A keyed lookup does
not prove an entire list is empty: assert the unkeyed list query itself (an unkeyed query takes no
`arguments`; each `result` row states the read-model identifier, else PLAY0351). A snapshot list does not render
(`STAGE-ESM-010`, Stage 4.73.1); an `observable` list does. Record that rendering limit.

## Ordering, lag and rebuild
- **Out of order across sources**: if a joined or cross-context fact can arrive first, add a
  family member with that order and say what the view shows meanwhile (joins never create rows).
- **Projection lag**: a spec asserts the settled state. If a screen decision depends on the view
  being current (a booking screen that must not offer a taken berth), that is not protection:
  the command needs its own constraint, or the rule is `recorded` as target-enforced.
- **Rebuild**: a view rebuilt from all facts must give the same rows. Mappings from
  `$eventContext.occurred` or other occurrence data cannot be executed at Screenplay v4.66.0:
  the plan blocks every event-context path except the event source id
  (`UnsupportedEventContext`). `given clock` does not change that; it supplies `$context.occurred`
  to command productions only. Record such projection cases as capability gaps (keep the
  mapping and the intended spec in design mode).
- **Corrections and backdated facts**: a correction is a new fact; specify the view after it.
  If a fact carries a business date earlier than its occurrence, specify which date the view
  shows.

## Performer-backed views (no events build them)
`given readmodel` (complete instance with its key) -> `when query` -> `then result` /
`then no result` / `then denied`. These are design-mode on most targets; check the mode.

## Cross-context views
Specs are not limited to one board chapter. Resolve the real event declarations across the
assembled application (qualify names where needed) and use them, with their real identities, as
givens. A quoted file import brings the declarations; an unquoted external-contract import does
not supply an unknown shape or portable execution. If a needed external shape, source identity
or capability is missing, record the dependency and the blocked scenario: do not invent a local
replacement event, drop a prerequisite, or count an empty-history spec as external coverage.
Empty history is valid only when it is itself the intended case (`...WithoutPartnerData`).

## Gated views
A query under a policy (including module and feature gates) needs `given caller` in every view
spec, and a denial spec: `then query <Q>` with `arguments` and no `result`, plus `then denied`
(or `when query <Q>` + `then denied`).
