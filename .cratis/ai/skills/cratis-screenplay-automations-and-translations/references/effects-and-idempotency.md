<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/effects-and-idempotency.md -->
# Effects and idempotency

## Three guarantees, three proofs
See "Effect guarantees and recovery traps" in SKILL.md.

## Where the external call sits
The model states facts; the call to the outside world happens in the realization of a command
or reaction. Choose and record one of:
1. **Effect, then fact** (`todo-list-example.md`): the realization calls out, then appends the
   result fact. Failure between the two repeats the effect on retry. Make it safe with an
   **idempotency key** the outside system honours (our stream id or a deterministic
   derivative), or accept at-least-once and say so.
2. **Intent fact, then outcome fact**: record our decision first (`CertificateRequested`), call
   out from the realization, and record the outcome from the outside system's
   acknowledgement through a `Translate` slice (`CertificateDelivered`, `DeliveryFailed`).
   Pending state is visible; reconciliation compares intents without outcomes.
3. **Outside system owns it**: we only record what they report (translation); the duplicate
   question moves to their side.
4. **Inline operation** (syntax-only: `system` + `operation` with `execute`/`compensate`
   are syntax-only and leave the application with no executable model): the command does
   immediate outside work before its facts commit and declares how to undo it if they do not
   (reserve a seat in an outside booking system); realized as an Arc `ICommandOperation`. Only
   when the outside work is part of the decision and has a meaningful reversal; never for
   work that must survive commit (email, payment, webhook): that is 1 or 2.
Payments, legal notices and anything costly or irreversible: prefer 2 or 3 and an idempotency
key; write a reconciliation case.

## Refusal disposition and redelivery

At Screenplay 4.125.0 an invocation can state ordered `on refused` selectors for
validation, constraint (optionally named) or explicit authorization denial.
Bare refusal does not include authorization. A matching branch declares a fact
or leaf `acknowledge`; `$refusal.reason`/`.message` are String details and
`.constraint` belongs only to constraint selectors. This is **authoring-only**
(PLAY0268), not evidence that a duplicate was safely acknowledged at runtime.

Recovery syntax `when redelivered <Event> to <Reaction>` selects exactly one
existing given occurrence; it is not append or replay. `then no events` makes a
no-new-facts contract explicit but remains unadmitted too. Full selectors,
occurrence matching and checked source examples: captures-and-reactions
`references/refusals-and-redelivery.md`.

Infrastructure/contract errors, Unsupported and concurrency are never domain
refusals. No `by concurrency` exists: fail/retry transient contention. Record the
actual target's disposition, trusted actor, durable delivery identity and external
idempotency/reconciliation owner. A branch or observer receipt alone proves no
cascade transaction or exactly-once external effect.

## Retry identity
- The retry must carry the same identity as the first attempt: the stream id chosen before
  the first try (never a fresh id per attempt), and the same idempotency key outward.
- Dedup scope must match the guarantee: `unique event` is per source; `unique x on` is across
  sources but lets the same source re-claim; capture change detection is per key and only as
  durable as the target's last-seen store.

## Ordering and lost acknowledgements
See "Effect guarantees and recovery traps" in SKILL.md.
- Facts are reacted to in append order in the reference; across sources assume no order.
- A capture spec does not acknowledge the source or store a new last-seen record; prefix
  facts that were accepted do not mean the record was processed (Screenplay `captures.md`).
  Record how the target acknowledges.
- Delivery guarantees, acknowledgements and durable fan-out are outside the language (Screenplay#286);
  never describe them as provided.

## Projection lag and rebuild
- The queue view may lag the closing fact: a sweep may see an item that was just closed. The
  invoked command's `unique event` absorbs that; the external call must too (idempotency key).
- Rebuilding the queue from history restores only genuinely open items, because closing facts
  are history too. A reaction must not repeat external effects on replay or rebuild: confirm
  the target's replay behaviour before relying on it, and record the answer.
See "Effect guarantees and recovery traps" in SKILL.md.

## What to write down (slice `documentation` + `STATE.md`)
- Effect placement (1-4) and why. Placement 4 is syntax-only: say the model then has no
  executable form.
- The idempotency key, and whether the outside system honours it.
- At-least-once accepted? Who notices duplicates or misses (reconciliation view, person)?
- The actor and gate decision for invoked commands.

## Chronicle realization
Choose replay exclusion when replay must not repeat an effect; an alternate replay handler
only when replay has different required work. Recovery redelivers normally either way. A
local delivery receipt cannot atomically prove an external service completed: keep external
idempotency or reconciliation.

A returned-event batch can be transactional across event sources; a reaction cascade is not
one transaction. Returned commands can fail validation or authorization, and a duplicate
append can fail its observer partition. Define when an already-completed result is
acknowledged and when a conflicting request must fail.

Separate domain failure from observer failure. A domain failure gets a timely visible
outcome and an owner; a paused or quarantined observer gets an operational recovery owner.

Prefer trigger facts as inputs. When an irreversible action depends on absence, do not trust
an eventually consistent sink: use authoritative state with an explicit protection strategy,
and bound unattended sweeps by expected population and per-run limits. Cross-service delivery
uses a published contract with outbox/inbox realization, not an assumed shared log;
namespace bridging is explicit translation.
