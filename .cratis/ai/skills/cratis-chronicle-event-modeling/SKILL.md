---
name: cratis-chronicle-event-modeling
description: Design a Chronicle event model before writing code - stream boundaries and event-source identities, commands, past-tense events, read models, automations and translations, compliance subjects, and the specification outline. Use when behavior, event vocabulary, stream boundaries, or a multi-slice flow is not yet settled. Do not use to draw or update an existing model diagram, and do not use for a mechanical change to a flow that is already decided.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-event-modeling/SKILL.md -->

# Chronicle event modeling

> **Model-first check.** If an accepted model under the model root covers this scope, or the
> repository is opted in (per `general.md` "Model first"; where `cratis-screenplay-modeling-lifecycle` is installed, run its `scripts/model-first-status.mjs`), the model is the source of truth: use
> `cratis-screenplay-event-modeling` (and the lifecycle in
> `cratis-screenplay-modeling-lifecycle`, which holds the master definition)
> instead of this skill. This skill is for
> hand-written C# models where the repository has not opted in (never force a
> model; only the entry-point session proposes one, at most once per session), and for
> code-level work against an already accepted contract (stream identity,
> compliance, specifications). If the repository is opted in and the Screenplay
> skills are not installed, report the missing capability and block the
> model-authoring work; do not write a competing brief here and never author
> model syntax from memory.

Use this **before writing code**, when behavior, event vocabulary, stream
boundaries, or a multi-step flow is not already settled. The output is an
implementation brief: which commands exist, which stream each event lands on,
which read models consume those events, which automations and translations react,
and which specifications prove the flow.

Skip it for a mechanical change where the event types and the flow already exist
and the request is wiring or a narrow fix.

> **Lineage.** The four behavior types and the given/when/then-per-behavior
> discipline follow **Event Modeling** (Adam Dymitruk; Martin Dilger,
> *Understanding Eventsourcing*). This skill applies that method to Chronicle.

## Verified product sources

Verified against **Cratis/Chronicle v19.50.1**:
`Source/Clients/DotNET.CodeAnalysis/DiagnosticIds.cs`,
`ProtectedValues/EncryptedAttribute.cs`, `EventSources/`, `Events/PublicAttribute.cs`,
`Sinks/PublishToAttribute.cs`, and `Events/Constraints/ClosesStreamAttribute.cs`
(paths under `Source/Clients/DotNET/` unless otherwise stated).
Tool versions: `cratis-screenplay-toolchain` `SKILL.md`.

| Package | Purpose |
| --- | --- |
| `Cratis.Chronicle` | event types, event-source identities, subjects, `[PII]`, `[Encrypted]` |
| `Cratis.Chronicle.CodeAnalysis` | `CHR0012`, `CHR0026`, `CHR0034`, `CHR0035`, `CHR0052` |

Reverify product sources before claiming support for another version.

## The four behavior types

Classify every behavior in the model as exactly one:

| Type | What it does | Built from |
| --- | --- | --- |
| **State change** | accepts a command, appends events | command, validation, events |
| **State view** | projects events into a queryable read model | read model plus a projection or a reducer |
| **Automation** | reacts to our own events and causes a side effect: an external call, a follow-up event (Screenplay `produces`) or a follow-up command (Screenplay `invokes`) | a reactor |
| **Translation** | takes data from outside our own facts (an external system or another service) and records it as our own facts | an adapter at the boundary, often a reactor |

The two differ in where the data comes from. An automation answers one of our own
events. A translation brings outside data in and keeps the external fact distinct
from the local one. A reactor that turns our own event into a follow-up event or
command is therefore an **automation**, however it is built. In Screenplay terms
these are the `Automation` and `Translate` slice types.

## Decide before implementing

- **Behavior name and type** for each step in the flow.
- **Commands** — their inputs and the authorization that gates them. Commands
  are imperative intents. For each rejection condition, name the values it reads
  as input, read state or context (time, caller, tenant). Context comes from the
  command pipeline or an injected time source, never from the wall clock inside
  the decision, so a specification can fix it.
- **Who issues each command**: a named role or an automation, never a generic
  user. Each role's "cannot" becomes authorization plus a denial specification.
- **Story order**: the flow from its first trigger to every ending, with
  branches and terminal facts named.
- **Refused transitions**: per state, which commands are refused and by which
  rule (a state-by-command table).
- **Events** — past-tense, one-purpose facts. Names must be self-describing:
  `<Entity><PastTenseVerb>`, never a bare `Created`. Name them for one real
  flow with real values. On a generic platform, model a case it handles
  (`HullDamageReported`), not its machinery (`FormSubmitted` with `content`).
- **The event-source id for every event.** Events never carry their own
  event-source id as a payload property; it lives in the event context. Deciding
  the id *is* deciding the stream boundary.
- **Event properties are non-nullable.** Analyzer `CHR0012` warns otherwise.
  Model an optional fact as a separate event, not a nullable field.
- **Read models** — their consumers and their source events, and whether the
  model is projection-backed, reducer-backed, or `[Passive]` for a
  strongly-consistent command-side decision.
- **Automations and translations** — which events (or which outside data) they
  react to, whether they emit follow-up events or run commands, and which side
  effects must not repeat.
- **Specifications** — the happy path, the validation failures, the constraints,
  the projections and reducers, and the reactor side effects.

Do not append an event for derived or aggregate state. Project it from the source
events instead; an event that records a total is a fact that can disagree with
its own inputs.

## Only domain facts, never repeats

Append only real domain facts. A tick, a heartbeat, a `…Requested`/`…Started`/`…Skipped`/`…Undecided`
event whose only job is to wake a reactor, a "last looked" timestamp, usage or cost telemetry and a
non-answer such as "could not decide" are not facts; orchestrate with a durable job, a state machine,
a reminder, a command operation or `ICommandPipeline` instead. An observation command (webhook, poll,
sweep, retry, classifier) compares with current state and appends nothing when nothing changed.
`…Changed` must be a real change, and a counter built from `[Increment]`/`[Decrement]` needs commands
that refuse non-transitions. Read [references/event-log-hygiene.md](references/event-log-hygiene.md)
when modeling an event vocabulary, an observation command or a counter, or measuring a log that grows
faster than the product's activity. Read
[references/event-log-cleanup-rollout.md](references/event-log-cleanup-rollout.md) when a release removes
or reshapes many event types and the stored data must be repaired.

## Public facts and streams that end

Declare source vocabulary with `IEventSource`, `[EventSource]` and `[EventStream]`;
use **cratis-chronicle-client-dotnet** for append routing and
**cratis-chronicle-reactor** for `[FromEventSource<T>]` filters. A stream that
ends needs an explicit lifetime and scope: does closing cover one source, a
period across sources, or every stream of a source type? Prefer a past-tense
closing fact with `[ClosesStream]`; use **cratis-chronicle-event-constraints** for
manual scope completion, reopening and its crash/reindex limits. Closing refuses
new appends; it does not erase history or stop projection replay. Specify a
late append refusal and an unrelated scope remaining open.

`[Public]` grants publication eligibility; `[PublishTo("sequence")]` selects the
projection/reducer event-output destination (outbox by default), not eligibility.
Owning-store membership can also grant eligibility, so private registration
visibility alone does not forbid publication. The event log is not a publication target. Translate inbox facts
into local facts at the boundary rather than treating external contracts as
local commands. Named tags are plaintext, indexed metadata: never personal data.

State-dependent decisions need **cratis-chronicle-decision-reads** when their
read facts must remain valid at append; an ordinary passive read alone does not
close the read/append race. Arc command integration belongs to
**cratis-arc-command-validation**.

## Information completeness — trace every field to an event

This is the check that pays for itself, and it belongs at modeling time rather
than after the projection misbehaves.

- **Backward:** for each read model, walk every property back to the event that
  carries it. A field with no source event is a **missing event or command** —
  not a nullable column. Resolve it in the model.
- **Forward:** every event you define should feed at least one read model,
  automation, or translation. An event nothing consumes is a smell: either a
  consumer is missing or the event should not exist.

If a field can only be filled by reaching into another behavior's read model to
*learn a fact the events should have carried*, you have found a missing event or
a wrong stream boundary. Fix the model. A read-only cross-slice read through
Chronicle is allowed where the rule needs it (an injected read model read by
explicit key, or a `[Passive]` projection for a current read; a rule that must hold under
concurrency needs read protection `On`, a stream decision or a constraint, see
[choosing how a rule holds](../cratis-chronicle-decision-reads/references/choosing-how-a-rule-holds.md)); see
Cross-slice patterns in `rules/vertical-slices.md`. Make such a read deliberate
and name it in the brief, with the consistency it needs: a rule that must hold
under concurrency needs enrolled guarded reads, not plain injection or a stale
materialized snapshot. In Arc use read protection `On` with the read model as a
validator or `Handle()` parameter for projected state, a per-stream read model or
`IStreamReads`/`StreamDecision` for one stream (`[ProtectedDecision]` +
`DecisionRead<T>` is the older per-command opt-in); `Result<,>` alone supplies
rejection, not a concurrency guard.

## Compliance modeling

Decide this *before* choosing event and read-model shapes, because erasure
operates on the subject and the subject follows the stream.

- Prefer **one subject per event stream** for person-level personal data. The
  subject then defaults correctly to the event-source id and needs no attribute.
- Mark inherently personal values with `[PII]` — names, email addresses, phone
  numbers, identity-provider subjects, personal notes. Leave business metadata
  unmarked; every marked value becomes unreadable once the subject is erased.
- Record the rationale with `[ComplianceDetails("...")]`. A string argument
  passed to `[PII]` is **not** read by Chronicle.
- Set `[Subject]` only when the person is not the event source. A stored
  read-model document carries one subject — do not mix several people's personal
  data into one document.
- Never place `[PII]` or `[Encrypted]` on an event-source id: Chronicle cannot encrypt it, and
  analyzers `CHR0034` and `CHR0052` reject them. When the natural identifier is itself
  sensitive, model a surrogate stream id and carry the sensitive value as a
  `[PII]` property.
- Do not place `[Key]` or `[Subject]` on an `EventSourceId<T>` value — it is
  already both, and analyzer `CHR0026` says so.
- Bearer tokens, magic links, and signed URLs are not durable facts. Model a
  keyed hash or an opaque reference, never the secret. Chronicle has no attribute
  that withholds a value from the log. Where an operational secret must genuinely be
  retained (an API key, a signing secret), mark it `[Encrypted]` (encrypted at rest,
  recoverable, not a PII erasure subject; never on a `[PII]` value or an event-source id)
  and keep it out of the causation chain with Arc's `[NotAudited]`; see `cratis-arc-command`
  and `cratis-chronicle-compliance`.

If a subject boundary cannot be made person-level without changing product
behavior, **stop and surface that trade-off** before implementing.

## Output shape

Write the brief in this order:

1. stream boundaries and subjects
2. commands and events
3. read models and their consumers
4. automations and translations
5. compliance notes
6. specifications

## The lifecycle: draft, ready, working, done

The behavior is the unit of work, and it moves through four states. The model —
events, commands, read models, screens, specifications — lives inside it, and
anyone may author or update it.

- **Draft** — being modeled; events, commands, read models, and boundaries are
  still in flux.
- **Ready** — the handoff gate. The model is *information-complete*: the checks
  above pass, and commands, authorization, compliance, and the specification
  outline are all decided. A ready behavior can be implemented with **no further
  modeling decisions**.
- **Working** — an implementer has picked it up and is running the
  implementation workflow end to end.
- **Done** — every quality gate is green.

**Marking a behavior ready is the signal to implement it.** Do not batch ready
items behind one another. Branching, pull requests, merging, and publication
remain separately authorized; a ready model authorizes none of them.

## Verify the brief before handing it over

- Every behavior has exactly one type.
- Every event is past-tense, self-describing, single-purpose, and non-nullable.
- Every event has a decided event-source id, and none carries that id as a
  property.
- Every read-model field traces back to an event.
- Every event has at least one consumer.
- Every cross-slice read is deliberate, read-only, goes through Chronicle, and
  is named in the brief with the consistency it needs; none stands in for a
  missing event.
- Personal data has a decided subject, and the subject is person-level or the
  trade-off is surfaced.
- No data-loading command (`LoadOrders`), UI event (`ButtonClicked`), technical
  event (`ApiCalled`) or recomputed event.
- Every state has its refusals listed.
- The specification outline names the failures, not only the happy path.

## Route near misses

- A model covers the scope or the repository opted in (see the check at the top): start from `cratis-screenplay-event-modeling`; do not write the brief here.
- Modeling the same system as a **Screenplay `.play` document** rather than as a
  brief for hand-written C#: `cratis-screenplay-event-modeling`. The method is the
  same; the artifact is a file that compiles, and the four behavior types above
  map onto its `StateChange`, `StateView`, `Automation` and `Translate` slices.
- Drawing a decided model as a diagram: `cratis-event-model-diagram`.
- A flow that crosses services, each owning its own event store: `cratis-multi-service-architecture`.
