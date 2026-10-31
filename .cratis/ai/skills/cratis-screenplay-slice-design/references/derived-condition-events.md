<!-- cratis-ai-managed: skills/cratis-screenplay-slice-design/references/derived-condition-events.md -->
# Derived-condition events

Read this when a view computes a condition that something else in the process might act on
("is this berth free right now", "has this application reached its next stage"), or when a read
model draws on many events and you ask whether one named fact is hiding behind them. It is a
business-modeling question, separate from the fan-in signal in `read-model-design.md`. Adapted
from Martin Dilger and Nebulit GmbH's orchestrating skill (see `provenance.md`), translated to
`reaction` and `produces`.

## The question

**Is this fact worth a new event?**

A derived condition earns its own event when both hold:
1. A domain expert would recognize and name it in its own right ("the berth was released"), not
   just as a field someone computes.
2. A later step needs it: another automation would react to it, another module would subscribe,
   or a downstream process must start from it.

If both hold, record it as its own event and let the projection and the downstream steps consume
that event, instead of recomputing the condition from several raw events everywhere.

If the condition is only displayed and nothing would ever act on it, it stays a read-model field
derived from the raw events, however wide the resulting fan-in looks. No new event is needed.

## It is not a fan-in fix
A read model that ends up fed by many events is at most a prompt to ask the question; it is never
the reason to answer it either way. The question is worth asking whatever the event count. Wide
fan-in on one field is handled by the checks in `read-model-design.md` ("Fan-in per field").

## Who produces the event
The derived fact is established by something that notices the condition, so it is produced by an
Automation slice: a `reaction` that listens for the raw events that can cause the outcome and
`produces` the new event (or `invokes` a command that does). Decide the shape by the usual
automation test (`cratis-screenplay-automations-and-translations`):
- An immediate, internal, always-possible reaction needs no pending-work view when its reason is
  recorded.
- Work that can wait, retry, fail or need people gets a pending-work view and a terminal fact, as
  for any automation.
- A reaction that both `reads` and `produces` directly fails binding (`PLAY0268`, decision 0006).
  When deriving the condition needs stored state, the reaction `invokes` a command that holds the
  `reads` and the decision, and that command produces the event. A reaction that `produces`
  directly may decide only from the triggering event's own fields.

## Fold only redundant causes
The causes of a derived condition usually fall into two groups:
- **Redundant for the outcome.** Several events that mean the same thing to the business
  (`BerthReservationReleased`, `BoatDeparted`, `BerthReturnedFromRepair` all mean "the berth is
  free again"). These are safe to consolidate: one `BerthMarkedFree` collapses N reasons into one
  reusable signal without losing information.
- **Distinct meanings the business still tells apart.** `BerthReserved`, `BoatArrived`,
  `BerthSentForRepair`, `BerthWithdrawn` each say why the berth is not free. Do not collapse them
  into a generic `BerthMarkedUnavailable`: that erases the why, and a downstream consumer may need
  it. Leave them as separate events; the fan-in that remains is not a defect, it means the events
  are individually meaningful.

Collapsing the second group is the modeling mistake, not the wide fan-in. Never rename or merge
persisted events to tidy a view (`cratis-screenplay-streams-and-consistency` `references/evolution.md`).

## Do not record clock-relative state
"Overdue", "expired" and "due in 3 days" change with the clock, not with a fact. Store the
deadline and compare at the query or screen edge. Record an event only when passing the deadline
is itself a business fact (a fee charged, a reminder sent), produced by a clock-triggered
reaction. See `read-model-design.md`, "No clock-relative state".

## Record the decision
Where the reason matters later, put it in the owning slice's `documentation`: which derived
condition, why it is (or is not) a fact worth an event, and which causes were folded and why.
