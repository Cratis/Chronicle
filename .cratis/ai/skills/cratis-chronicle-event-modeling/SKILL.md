---
name: cratis-chronicle-event-modeling
description: Design a Chronicle event model before writing code - stream boundaries and event-source identities, commands, past-tense events, read models, automations and translations, compliance subjects, and the specification outline. Use when behavior, event vocabulary, stream boundaries, or a multi-slice flow is not yet settled. Do not use to draw or update an existing model diagram, and do not use for a mechanical change to a flow that is already decided.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-event-modeling/SKILL.md -->

# Chronicle event modeling

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

This skill is verified against these exact sources:

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Chronicle` | `16.45.2` | event types, event-source identities, subjects, `[PII]` |
| `Cratis.Chronicle.CodeAnalysis` | `16.45.2` | `CHR0012`, `CHR0026`, `CHR0034`, `CHR0035` |

Reverify product sources before claiming support for another version.

## The four behavior types

Classify every behavior in the model as exactly one:

| Type | What it does | Built from |
| --- | --- | --- |
| **State change** | accepts a command, appends events | command, validation, events |
| **State view** | projects events into a queryable read model | read model plus a projection or a reducer |
| **Automation** | reacts to events and causes an external side effect | a reactor |
| **Translation** | reacts to events and appends follow-up events elsewhere | a reactor |

Automation and translation differ in the *result*: an automation reaches out of
the system, a translation stays inside it and produces new facts.

## Decide before implementing

- **Behavior name and type** for each step in the flow.
- **Commands** — their inputs and the authorization that gates them. Commands
  are imperative intents.
- **Events** — past-tense, one-purpose facts. Names must be self-describing:
  `<Entity><PastTenseVerb>`, never a bare `Created`.
- **The event-source id for every event.** Events never carry their own
  event-source id as a payload property; it lives in the event context. Deciding
  the id *is* deciding the stream boundary.
- **Event properties are non-nullable.** Analyzer `CHR0012` warns otherwise.
  Model an optional fact as a separate event, not a nullable field.
- **Read models** — their consumers and their source events, and whether the
  model is projection-backed, reducer-backed, or `[Passive]` for a
  strongly-consistent command-side decision.
- **Automations and translations** — which events they react to, whether they
  emit follow-up events or run commands, and which side effects must not repeat.
- **Specifications** — the happy path, the validation failures, the constraints,
  the projections and reducers, and the reactor side effects.

Do not append an event for derived or aggregate state. Project it from the source
events instead; an event that records a total is a fact that can disagree with
its own inputs.

## Information completeness — trace every field to an event

This is the check that pays for itself, and it belongs at modeling time rather
than after the projection misbehaves.

- **Backward:** for each read model, walk every property back to the event that
  carries it. A field with no source event is a **missing event or command** —
  not a nullable column. Resolve it in the model.
- **Forward:** every event you define should feed at least one read model,
  automation, or translation. An event nothing consumes is a smell: either a
  consumer is missing or the event should not exist.

If a field can only be filled by reaching into another behavior's read model,
you have found a missing event or a wrong stream boundary. Fix the model; do not
cross-read at runtime.

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
- Never place `[PII]` on an event-source id: Chronicle cannot encrypt it, and
  analyzer `CHR0034` rejects it. When the natural identifier is itself
  sensitive, model a surrogate stream id and carry the sensitive value as a
  `[PII]` property.
- Do not place `[Key]` or `[Subject]` on an `EventSourceId<T>` value — it is
  already both, and analyzer `CHR0026` says so.
- Bearer tokens, magic links, and signed URLs are not durable facts. Model a
  keyed hash or an opaque reference, never the secret. Chronicle has no attribute
  that withholds a value from the log.

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
- No behavior depends on reading another behavior's read model at runtime.
- Personal data has a decided subject, and the subject is person-level or the
  trade-off is surfaced.
- The specification outline names the failures, not only the happy path.
