<!-- cratis-ai-managed: skills/cratis-screenplay-slice-design/references/read-model-design.md -->
# Read-model design

## Contents

- Start from the consumer
- The typical slice pattern
- Events or views
- Common view patterns
- Components
- Fan-in per field
- Builder and freshness
- Query shape follows the business view
- No clock-relative state
- Who may see what
- Collections and many-at-once
- Building it
- Omissions are decisions
- Checks before handing off

## Start from the consumer

List every consumer before designing any read model: each screen area a person looks at, and
each automation that needs to decide something. A read model exists for a consumer; a read
model nobody uses is a question.

For each consumer: what does it show or decide, for which instance or set, and for whom? How
stale may it be, and what does a stale answer cost? For each field: what does the person decide
or do with it? A field with no decision behind it is a question, not a requirement.

A screen that acts on an existing instance needs a view: it supplies the identifier the command
needs and the state the person decides on. Only a creation screen with nothing to show is
exempt; state the exemption.

## The typical slice pattern

Most screens follow `read model -> screen -> command -> event`: the read model feeds the
screen, the screen triggers a command, the command produces an event. Not only status screens:
a command screen that shows current state before the person acts (a berth booking form that
shows the berth's availability, an invoice payment form that shows the amount still owed) also needs its view. A pure
view screen is the shorter `read model -> screen`. An automation whose work can wait, retry,
fail or need people is `read model -> automation -> command -> event`, where the read model is
its todo list; an immediate, internal, always-possible direct reaction needs no todo list when
its reason is recorded (`cratis-screenplay-automations-and-translations`). Treat a screen with
no incoming view as a gap; only a blank creation form with no prior state to show is exempt.

## Events or views

The most important distinction in event sourcing: events are immutable facts someone caused;
views are derived from them and may change every time a source event arrives. Ask in order:

| Question | Yes means | Example |
|---|---|---|
| Did an actor decide or do something? | event | a member confirmed the membership |
| Is it pure calculation from facts already recorded? | view | berths free, total invoiced |
| Does it recalculate whenever source facts change? | view | outstanding balance |
| Is it derived from several other events? | view | membership status |

A calculation is not the business act that adopts it. Running or recomputable figures (totals,
balances, counts, averages, search indexes) are views, never events. But a value someone agrees
or certifies stays a fact: an invoice total the customer accepted (`InvoiceIssued` carries it),
a balance an accountant certified (`BalanceCertified`), a quoted price, a set fee. Test: if the
source facts change, should the figure change with them (view) or stay as decided (fact)? Wrong:
`BerthsFreeRecalculated` with `free = 84` (it recalculates). Right: a `BerthAvailability` view
built from `BerthBooked`, `BookingCancelled` and `BerthAdded`. History of a running figure is
kept in a separate view, not as events.

Classify what an automation outputs. A new fact it establishes is an event (`PaymentTaken`). A
pure calculation is projection or reducer work, built from events with lineage
(`cratis-screenplay-projections`), never a direct write by the automation. A notification is an
external effect: it always gets a delivery outcome, a closing fact (sent, failed, abandoned)
and a recovery contract, or a pending-work view; only an immediate, internal, always-possible
effect may leave no trace
(`cratis-screenplay-automations-and-translations`).

## Common view patterns

Four shapes recur. Name the one a view is before choosing its fields, so the consumer and the
events line up.

| Pattern | Events feeding it | The view holds | Used for |
|---|---|---|---|
| Status view | The lifecycle events of one instance (booked, confirmed, arrived, cancelled) | The accumulated current state of that instance | A screen that shows where one thing stands and which actions are available |
| List view | Create, change and end events of many instances | One summary row per instance, with the fields people filter, sort or search by | Worklists, search, filtering; end events either `remove with` the row or set a status literal (see "Omissions are decisions") |
| Timeline or history view | Any events that carry a time | A chronological list, one entry per event, kept apart from the status view | History and audit trail; keep a growing history in its own view, never inside the status view |
| Processor decision view | The state-changing events a reaction cares about | Current state only, nothing a person would read | A `reaction` or `trigger` that reads it to decide the next action; a reaction that also `produces` directly cannot `reads` it (`PLAY0268`), so it `invokes` a command that holds the `reads` (a pending-work view is this pattern for work that can wait, retry or fail) |

The patterns describe what the view is for, not new syntax: each is a `readmodel`, a projection
and, where a person or caller reads it, a `query`. A view that fits two patterns at once is
usually two views.

## Components

- A component is an area of a screen a person would describe as one thing ("the locker list",
  "my request status"). Most screens have one.
- One read model per component. Split when a person would point at two areas and name them
  differently, or when one area needs facts from many events and another needs only one or
  two - they change for different reasons and at different speeds.
- A homogeneous list is one component, even when its rows draw on many event types (a berth
  list whose status is set by booking, cancellation and maintenance events). Do not use that
  as a reason to fold in fields without the same wide fan-in: a berth's name and pier (set by
  one or two events) and its live availability (derived across its whole lifecycle) are two
  kinds of computation even on one page. Split them when you can name the consequence; if
  you keep one view, note which field has the irreducible fan-in and why (in the slice
  `description`).
- A screen with several components has one `data` line per component, each
  `data <RM> via query <Q>` from its own read model (several `data` lines per screen are
  valid; `cratis-screenplay-read-surface`). A split component sits next to its own source
  events instead of one view reaching across the whole timeline.
- Do not re-derive a view from a screen's title: take its fields, group them by the read model
  each draws from, and build the read model from that grouping.

## Fan-in per field

For each read-model field, count the events that set it. More than about three is a signal to
look at, not a rule: a status fed by a long lifecycle is normal. Ask whether the field mixes
concerns that should be separate views, and whether every contributing event is genuinely a
change to *this* value. Report a problem only when it has a consequence (coupling, a view that
must change whenever unrelated facts change).

## Builder and freshness

Observable delivery does not make a materialized projection decision-consistent; a protected
decision needs a guarded read (`rule-layers.md`, `cratis-screenplay-streams-and-consistency`). Prefer a
declarative projection (mappings, event joins, counters, bounded children, removals); a join
consumes event facts, not another read model. Use a reducer when prior-state, attempt or
ordering guards cannot be expressed declaratively. Review every writer of a property: AutoMap
can introduce updates from other events. Use variants only when lifecycle stages have different
shapes. Keep growing histories in a separate view; never combine several people's PII under
one read-model instance (compose queries across person-scoped views instead).

Every `from` event must set or affect a field (AutoMap counts); otherwise drop it or record why
it stays.

## Query shape follows the business view

| The person... | Design-mode query | Executable/renderable scope |
|---|---|---|
| opens one thing | `XById => RM optional` + `by xId XId` | binds at 4.135.0 with caller-supplied `by`; check target renderer |
| scans a list | `ListX => RM[]` (optionally `observable`, `filter`) | collection/observable delivery bind; caller-supplied equality filters bind since 4.143.0 as ESM v14; check target renderer |
| sees their own rows | list with scope/`from` on the caller | scope and context-derived `by`/`filter` remain PLAY0268 because portable scope/query context is unadmitted; caller-supplied filters are not access control |
| sees a site-wide figure | singleton read model, `from <Event> key literal "global"` on every `from` (a projection-level `key` routes nothing, PLAY0381; example in Screenplay `projections/keys.md`, "Literal keys") | check the binding (V3, `cratis-screenplay-toolchain`) |

- `observable` when people are expected to see others' or automation's changes while looking
  (worklists, status awaiting an outside answer); one-shot otherwise. Observable delivery
  binds at 4.135.0; renderer support is separate. If access can be revoked mid-session,
  record that the target re-checks per emission.
- Never model paging or page size (no syntax, Screenplay#140). State the order in the query
  `description` ("oldest first"; queries do not accept `documentation`); an unbounded list is a review question.
- A gated keyed query needs its own denial spec.

Never drop a business-required list query to satisfy an old compiler or renderer;
record any actual capability gap. At 4.135.0, explicit trailing `key` takes precedence
for read-model identity. Otherwise single-instance queries in the owning slice must
agree on one `by` property declared on the read model. Only with no single-instance
identifier and an admitted collection/list query does conventional `*Id` inference
run; queryless views need explicit `key`. The fallback excludes collection-query key
candidates when that leaves one identifier (full algorithm: read-surface
`references/named-keys.md`). The identifier equals the projection's effective key:

| Effective key | Executable model | Renderable model (Stage) |
|---|---|---|
| event source (default or `key $eventSourceId`) | `xId = $eventSourceId`, or leave it unmapped in a scoped projection | scoped: leave it unmapped; the event must not carry a same-named, same-typed property unless `no automap` |
| event property (`key invoiceId`) | `xId = invoiceId` (never `$eventSourceId`) | if it binds **flat** (exact condition: `admission.md`, "Identity and key rule" 3), Stage admits it only with one `from`, every property mapped from an event property (no literals) and the key an identifier concept filled by each producing command's `for`; any other flat-bound shape is refused (`STAGE-ESM-008`/`-009`), it does not fall back to scoped. Any other shape is scoped (for example a `remove with`, a `= null` clear, or another `from` keyed by the event source); then leave the identifier unmapped or map it only from the key's own property |
| literal (`key literal "global"`) | leave it unmapped (scoped seeds it from the key) | refused: Stage keys are an event property or the event source (`STAGE-ESM-017`); keep the singleton in design or executable scope and record the gap |

The full rule, with the sources, is in `cratis-stage-rendering-and-sandbox`
`references/admission.md` ("Identity and key rule").

## No clock-relative state

"Overdue", "expired", "due in 3 days" are true now and false later; a view changes only when a
fact arrives. Store the deadline (business-supplied) and compare at the query or screen edge.
Record a fact only when passing the deadline is itself a business fact (a fee charged, a
reminder sent): a clock-triggered reaction (`cratis-screenplay-automations-and-translations`).

## Who may see what

- `by` and `filter` are values the caller chooses. Anything the caller must not choose (their
  own identity, the tenant) comes from context (`from $context...`), never from a filter.
- `scoped to global` reaches past the tenant; treat it as a review question.
- A compiling query is not access control; the target realises it.

## Collections and many-at-once

Ask for each field: can there be more than one at the same time? If yes, the view is a list or
has children, not a single value.

## Building it

- Event properties fill read-model properties of the same name automatically (AutoMap), but the
  match differs by binding: a scoped projection matches names case-insensitively and only for
  the same type (Screenplay v4.66.0 `ProjectionValues.cs` `AutoMapped`); a flat-bound projection
  matches exact names only and does not check the type, so a same-named property of another
  type makes the model invalid (`Projections.cs` `BindFlatTransition`). Do not rely on AutoMap
  where it matters: map explicitly when names or types differ, and use `no automap` where an
  event property would land on the identifier.
- Status words are literals per event (`state = "waiting"` on the request event).
- Removal is a fact: `remove with <Event>` when the business ends the instance's visibility.
- Projection mechanics (keys, joins, children, counters): `cratis-screenplay-projections`.
- A read model that is not built from events uses a query `performer`; its data is outside the
  model's lineage - say where it comes from.

## Omissions are decisions

If a view must stop showing cancelled items, use `remove with <Event>` and pin it with
`then no readmodel <RM> for "<key>"`. If it keeps them, map a status literal from that event
instead. The slice `description`, the projection and every spec of the view must agree.
A complete example (marina berths; the keyed query supplies the view's identity
for the `then no readmodel` assertion; explicit `key` could supply it instead):

```screenplay
domain Harbourlight.Berths

concept BookingId : Uuid
concept BerthName : String
  validate
    not empty  message "A berth needs a name"
concept BookingState : Enum
  open
  cancelled

policy IsAuthenticated
  require authenticated

module Berths
  description "Berth bookings for one marina"
  authorize IsAuthenticated
  feature Bookings
    slice StateChange BookBerth
      description "A skipper books a berth"
      command BookBerth
        bookingId BookingId identifier
        berth BerthName
        produces event BerthBooked
          berth BerthName = berth
      specification BookingABerth
        given caller
          authenticated
        when BookBerth
          bookingId = "3b0c7a14-5d2e-4f61-8a9b-0c1d2e3f4a01"
          berth = "Pier 2, berth 14"
        then BerthBooked
          for "3b0c7a14-5d2e-4f61-8a9b-0c1d2e3f4a01"
          berth = "Pier 2, berth 14"

    slice StateChange CancelBooking
      description "A skipper cancels a booking they no longer need"
      command CancelBooking
        bookingId BookingId identifier
        produces event BookingCancelled

    slice StateView OpenBookings
      description "The harbour office lists bookings that still hold a berth. BerthBooked sets berth: the booking enters the list. BookingCancelled removes it: a cancelled booking never shows."
      readmodel OpenBooking
        bookingId BookingId
        berth BerthName
      query OpenBookingById => OpenBooking optional
        by bookingId BookingId
      projection OpenBookings => OpenBooking
        from BerthBooked
          bookingId = $eventSourceId
          berth = berth
        remove with BookingCancelled
      screen OpenBooking
        title "Open booking"
        data OpenBooking via query OpenBookingById
        action CancelBooking
      specification CancelledBookingLeavesTheList
        given caller
          authenticated
        given BerthBooked
          for "3b0c7a14-5d2e-4f61-8a9b-0c1d2e3f4a01"
          berth = "Pier 2, berth 14"
        given BookingCancelled
          for "3b0c7a14-5d2e-4f61-8a9b-0c1d2e3f4a01"
        then no readmodel OpenBooking for "3b0c7a14-5d2e-4f61-8a9b-0c1d2e3f4a01"
```

## Checks before handing off

- StateView slice `documentation` gives, per contributing event, the fields it sets and why
  (see `LockerBoard` in `worked-example.md`); extend it whenever a `from` is added. Shape of
  the note: `BerthBooked sets bookingId, status="booked": the view's creation event; the booking
  does not exist before it.` / `BoatArrived sets status="arrived", arrivedAt: the only
  event carrying an arrival time.` / `BookingCancelled sets status="cancelled": terminal.`
  Re-check every field against the fan-in signal each time a `from` is added; an earlier note
  about one field does not clear the others.
- Before completing, re-read every read model in scope from the `.play` files and recheck each
  field and each reason note, including roll-up views, AutoMap-supplied fields, joins and
  fields added since: a note about one field never clears the others. Apply the named-
  consequence threshold above, not a blanket split.
- Every screen and automation re-checked one by one, not from an earlier list: connected to a
  view, exempt (blank creation form with reason stated; direct reaction with its documented
  no-pending-work reason), or a gap fixed or reported.
- Every consumer that needs state has its read model (a blank creation form and a direct reaction
  with a documented no-pending-work reason need none); every read model has a consumer.
- Every field traced (`field-lineage.md`).
- Executable scope: one identity per read model: explicit `key`, else agreeing
  single-instance `by`, else unambiguous `*Id` only with an admitted list query and
  no single-instance identifier. Queryless views need explicit `key`. The identifier
  equals the projection's key (table above); check target renderer admission separately.
- Design-only shapes listed with the V3 code they cause.
