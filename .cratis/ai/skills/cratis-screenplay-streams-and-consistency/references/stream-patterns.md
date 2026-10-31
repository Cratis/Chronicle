<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/stream-patterns.md -->
# Stream patterns

## Contents

- Identity and event-membership review
- Interpret the evidence
- Event meaning is a separate check
- Contrasting examples
- Boundary decision tree
- Red flags that the boundary, not the volume, is wrong
- Boundary patterns by domain
- Core rules that bear on stream design
- Growth and snapshots
- Short lifecycles
- Consistency questions per stream
- Constraint scope and what it implies for stream choice
- Decision-scoped consistency (note)

A stream in Screenplay is the event source a production lands on: `identifier` on the
command, `for <identifier>` on each `produces`, `$eventSourceId` in projections, `for "<id>"`
in specifications. Choosing the identifier *is* choosing the stream.

Composite stream keys declare `streamId` with two or more
named scalar parts and map every part on routes (specification parts are literals).
Scalar forms remain valid. Text is nonempty, well-formed NFC; Double integer ids
stay within ±(2^53−1). Protected concepts cannot be key types/parts or mapping
sources (PLAY0515); choose surrogate keys and keep protection in payloads.
Canonical encoding formats parts, escapes `%`/`|`, and joins in declaration order;
never split hand-joined legacy text by guesswork. A stored key-schema change needs
a new stream identity or migration. See toolchain `references/sources-and-streams.md`.

## Identity and event-membership review
Start from the business instance, then choose its representation. A generated id can name a
real instance; a readable constant can hide a collection. Write one sentence per identity
concept:

> `EnrolmentId`: one stream per enrolment of one member in one course; it answers "what is
> the state of this enrolment?"

For every affected identifier:
1. Name the instance and its lifecycle.
2. Inspect every producer and destination that can write its history, including reactions and
   captures (`cratis-screenplay-automations-and-translations`), not only event declarations.
3. Explain how each event changes or records that instance's history.
4. List cross-stream references (payload properties) and the consumers that need them.
5. Decide: retain, split, model a view instead, or leave unresolved pending a named answer.
   Record a short reason and the invariant a split would affect; do not build a second catalog.

Facts about another thing go on that thing's stream with a reference as payload
(`MemberEnrolled` carries `courseId` and `memberId`). A view over many streams is a projection
keyed by the reference (`from MemberEnrolled key courseId`), never a shared stream. A command
with no identifier allocates a new stream (executable mode needs explicit allocation, see
`cratis-screenplay-toolchain`). Prefer an identifier the caller chooses before the first attempt: it
is also the retry identity; one allocated afresh per retry (including a `generated identifier`, which is generated on every acceptance) can create a second history.
Never use personal data as the identifier (see `chronicle-boundaries.md`).

### Event-by-event identity check (record the result)
Do not stop at the identity concept's name. Walk every event, command production, reaction and
capture that writes to the identifier and fill one row each:

| Event | Written by (command, reaction, capture) | How it changes or records this instance's history | Belongs? |
|---|---|---|---|
| `MemberEnrolled` | `EnrolMember` | records the enrolment decision | yes |
| `CourseSeatsAdjusted` | `AdjustSeats` (same identifier) | changes the course, not the enrolment | no: move to the course stream |

Then decide per identity, with a reason and the invariant a split would affect:
- **retain**: every event is a fact about this instance, however long the history;
- **split**: some events have another lifecycle or decision owner; name the new identity and
  re-check every rule and reference that crossed the split;
- **view instead**: the "stream" is really a collection or report; keep facts on the real
  instances and build the combined view by projection;
- **unresolved**: record the question, the person who can answer and the default taken meanwhile.

## Interpret the evidence
| Observation | Investigation and possible decision |
|---|---|
| Events describe one business instance | Retain, however long the history. |
| Parent and children decide together and share a lifetime | A small composite may be one stream; test shared invariants and whether children live independently. |
| Independent instances appended together only for reporting | Give each its own identity; build the combined view from facts. |
| A named collection makes its own decisions | It may be a real entity: separate its membership facts from its members' histories. |
| Unrelated technical happenings share a source | An audit or observability log is not a domain decision boundary; split by business concern or leave to observability. |
| Groups of events with independent lifecycles (profile vs sessions) | Consider one identity each, then re-evaluate rules and references that cross the split. |
| A closed instance still needs its history | Retain the identity by default; different access frequency alone is no reason to split. |
| Archive custody has its own business lifecycle | Consider a linked identity for custody facts; preserve the original history. |

Length, age, event count, a conjunction in a description or a collection-like name each fail to
prove a boundary wrong: a thirty-year account is correctly one long stream. Report a defect only
with the lost business meaning, lifecycle independence or consistency that follows.

## Event meaning is a separate check
A correctly routed event can still be unclear. Do not name an event or command for a form:
`Update/Edit/Save/Set/Change/Manage<Noun>` and `<Noun>Updated/Changed/Edited/Saved/Modified` are
not accepted names. Name the business act, or for a correction say what it corrects
(`ResultCorrected`); ask which decision happened and whether a consumer can tell it from other
changes without comparing snapshots (naming method: `cratis-screenplay-discovery`, `cratis-screenplay-slice-design`). Do not split every property into an event, and do not merge recognised facts
because one command produced them. Separately, an event whose payload restates the creation event is a review signal (major only when
meaning is lost, a rule is bypassed or consumers become ambiguous); historical generations are
exempt from that signal, not from the naming rule.

## Contrasting examples
- A research study may accumulate observations for years; duration does not invalidate the study
  identity, yet participants or lab analyses may have their own lifecycles.
- A diagnostic panel request may own several measurements finalized together; a measurement that
  can be commissioned and repeated alone suggests its own identity and a rule joining results.
- "All analyses received today" is a query; a scheduled analysis batch with its own authorization,
  capacity and completion decisions can be a real instance.
- Closing a study needs no rewrite into an ArchivedStudy stream; a separately managed legal-custody
  case may warrant a linked identity.

## Boundary decision tree
Work top to bottom for each stream; stop at the first answer that applies.

```text
Does the stream have a natural business identity (one booking, one invoice, one member)?
 no  -> it is a collection or a log: keep facts on the real instances, build a read model
 yes -> does every event in it belong to that one instance's own lifecycle?
         yes -> boundary is right, however long the history gets
         no  -> the boundary is too wide: split by the instance each event is about, then
                re-check every rule and reference that crossed the split
```

## Red flags that the boundary, not the volume, is wrong
Each is a signal that needs the lost business meaning named before it is a finding:
- events with no shared business meaning (a technical log, `SystemMetricRecorded`): use an
  observability system or a view, not a domain stream;
- one stream holds several unrelated instances (every member's events under one constant id);
- nobody can say which single business question the stream answers, or the answer is a list
  of unrelated questions: look for a collection or a log;
- a stream is appended to for a reporting need, not a business decision.

Do not use "no natural end" on its own as a red flag: an open membership has none and is still
one identity.

## Boundary patterns by domain
Each entry names the identity and the question that tests it. Durations are not part of the
test; they vary by business.

| Domain | Identity (one stream per) | Test question | Counterexample to watch for |
|---|---|---|---|
| Invoicing | invoice | Is every event a fact about this invoice (issued, line added, paid, credited)? | an "all invoices of the month" stream: that is a query |
| Invoicing | payment attempt | Can a payment be attempted, refused and retried independently of the invoice's other decisions? | folding attempts into the invoice makes retries race the issuing decisions |
| Marina | berth booking | Does the booking have one owner (vessel) and a natural end (departure, cancellation)? | a berth stream that holds the whole stay history (billing, vessel changes): keep the booking's own history on the booking |
| Marina | berth (as a managed resource) | Does the berth itself make decisions (closed for repair, re-priced) or must occupancy be decided atomically (one vessel at a time)? | none for reserve and release facts: they may belong on the berth when it is the authoritative decision point (`consistency-and-concurrency.md` section 3); carry the booking id as a reference and keep the booking's own history on the booking |
| Memberships | membership | Is it one lifecycle (joined, renewed, lapsed, ended)? A thirty-year membership is still one stream | profile, preferences and login history bundled: three concerns with different decisions; split when they have independent lifecycles |
| Memberships | "all members" | none: it is a collection | model as a view |
| Course enrolment | enrolment | One member in one course, from enrolment to completion or withdrawal | a "course" stream that also carries every member's progress |
| Hire | hire agreement | From reservation to return, one customer and one item | an "active hires" stream beside a "completed hires" stream: lifecycle stages of one instance, not two identities |

A composite can be one stream when parent and children are decided together under shared
invariants (an invoice and its lines: lines are added and removed under invoice-wide totals and
status rules). Examine those shared invariants first. Split a child only when it has its own
business identity, its own decision authority and its own lifecycle, not merely because it is
created, changed or removed separately: then the child is an identity and the parent holds a
reference.

## Core rules that bear on stream design
- **Facts are immutable; a correction is a new event.** Never design a stream that needs an
  edit of history to be right (`consistency-and-concurrency.md` section 9).
- **No calculated events.** An event whose value is recomputed as source data changes (a running
  total, an average, "seats left") is a read model, not a fact. The exception is a calculation
  the business itself recorded at a point in time (the price quoted, an assessment): that is
  a fact, and later recomputation must not rewrite it.
- **Every command has an explained business origin.** A human action, a reaction to any
  modeled event, or a clock or application trigger can invoke a command; an external capture
  starts a process by appending facts, and any later command is reached through a reaction. Name
  the origin; do not infer a missing slice merely because no prior event exists. An origin
  nobody can explain (a command no persona or reaction issues) is the finding.
- **Open question versus decided failure.** An unanswered ownership or consistency choice is
  an open question: keep it visible in `STATE.md` with an owner. A rejection whose behavior is
  decided (the second claim is refused with a fixed message) is specified behavior: write it
  as a `then error` specification, not as a lingering note that makes it look unresolved.
- **Fan-out is a signal.** One command producing more than two events may be doing more than
  one job, or may be one outcome that always happens together. Check whether the facts can
  occur independently before reporting it; the business may recognize all of them.
- **Offline first.** Model how the work would run with people and paper before streams: a
  step that exists only because of the system (cache refresh, session check) is not a fact
  and needs no stream.

## Growth and snapshots
Design first, snapshot second. If a stream seems too long, first ask whether the identity is wrong
(split by the narrower business instance), whether events are too fine-grained, and whether
decisions read from a projection rather than replaying history. Snapshotting, caching and replay
speed are target concerns: Screenplay does not model them, and the model must never grow a stream
or an event only to make replay cheaper. Estimate before worrying: events per instance per year
times the instance lifetime. Measure real replay latency in the target before adding any snapshot
mechanism, and name the commands that actually replay the stream rather than read a view. A
thirty-year account is correctly one long stream; an eternal "all orders" stream is not.

## Short lifecycles
When the business has natural ends (a period, an attempt, a case, a booking), a stream per
end-bounded thing keeps streams short, keeps evolution cheap (old shapes stop being written
soon), and narrows races. Do not invent artificial periods where the business has none.

## Consistency questions per stream
Ask for each stream:
1. Which rules must hold *at the moment of appending* to this stream? (enforce here)
2. Which rules span several streams? (constraint, or recorded state-dependent rule)
3. Who else writes to this stream, and can they race? (concurrency scenario)
4. What does a retry of each command do? (retry identity, `unique event`)
5. Which views read it, and what may they show late? (projection lag)

## Constraint scope and what it implies for stream choice
- A Screenplay constraint covers the whole event sequence across all sources; narrower scope
  (per source type or stream) cannot be declared yet.
- `unique <prop> on <Event>`: the value belongs to one source; that source may re-claim it,
  and a new value from the same source frees the old one. Nulls are skipped.
- `unique event <Event>`: at most once per source. Several `unique event` lines under one
  name are mutually exclusive until `released by`. This is how simple lifecycle rules
  ("one result per attempt", "withdraw once") can be enforced atomically today.
- So: if a rule is "one per X", make X's stream the source and use `unique event`; if it is
  "no two Xs share Y", put Y on the event and use `unique Y on`.

## Decision-scoped consistency (note)
Some event stores decide consistency by a query over tagged events instead of one stream
(often called dynamic consistency boundaries). Chronicle's concurrency scopes can narrow a
check to event types, source type or stream (`concurrency` block, Screenplay
`commands.md`). Legacy concurrency metadata still fails binding (PLAY0271),
but sources/streams/routes bind and reference-execute as ESM v8 on 4.125.0.
Route admission does not make a view read protected. The CLI (toolchain `references/renderable-subset.md`) reaches Stage construct checks
through v10. Model the stream by business
identity; when a rule truly needs a cross-stream decision, record it (see
`consistency-and-concurrency.md`) rather than inventing a construct.

Complete compiled example: `streams-example.md`.
