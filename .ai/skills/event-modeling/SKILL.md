---
name: event-modeling
description: Design a Cratis event model before writing code — decide stream boundaries, commands, events, read models, automations/translations, compliance subjects, and the spec outline. Use this when behavior, event vocabulary, stream boundaries, or a multi-slice flow is not yet settled, before implementing a slice.
---

# Event Modeling

Use this skill **before writing code** when behavior, event vocabulary, stream boundaries, or a multi-slice flow is not already settled. The output is an implementation brief: which commands exist, which stream each event lands on, which read models consume those events, which automations/translations react, and which specs prove the flow. Afterward, use the `create-event-model` skill to draw or update the Mermaid `EventModel.md` diagram.

> **Lineage.** Cratis's four slice types and the Given/When/Then-per-slice discipline follow **Event Modeling** (Adam Dymitruk; Martin Dilger, *Understanding Eventsourcing*); this skill applies that method to Cratis.

Skip this only for mechanical changes where the event types and flow already exist and the request is just wiring or a narrow fix.

## The brief — decide before implementation

- **Module / feature / slice name and slice type** for each behavior (State Change / State View / Automation / Translation).
- **Commands:** inputs and the authorization (roles/policy) that gates them. Commands are imperative intents.
- **Events:** past-tense, one-purpose facts. **Decide the event source id for every event** — events never carry their own event-source id as a payload property. **Event properties are non-nullable** (Chronicle's analyzer warns otherwise); model optional facts as *separate* events, not nullable fields. Don't append events for derived/aggregate state — project that from source events.
- **Read models:** their consumers and source events; whether projection-backed, reducer-backed, or `[Passive]` (command-side decision only).
- **Automations / translations:** which events they react to, which side effects need `[OnceOnly]`, and whether they emit follow-up events or run commands via `ICommandPipeline`.
- **Specs:** happy path, validation failures, constraints, projections/reducers, reactor side effects.

## Information completeness — trace every field to an event

The core Event Modeling check, run at modeling time (not after the projection misbehaves):

- **Backward:** for each read model, walk every property back to the event that carries it. A field with no source event is a **missing event or command** — not a nullable column. Resolve it in the model before implementing.
- **Forward:** every event you define should feed at least one read model, automation, or translation. An event nothing consumes is a smell — either a consumer is missing or the event shouldn't exist.

If a field can only be filled by reaching into another slice's read model, you've found a missing event or a wrong stream boundary — fix the model, don't cross-read at runtime.

## Compliance modeling (when personal data is involved)

Decide compliance *before* choosing event/read-model shapes:

- Prefer **one-subject event streams** for person-level PII. If an event carries PII about a natural person, decide the subject explicitly.
- Use **concept-level `[PII]`** for inherently personal values (names, email, phone, identity-provider subjects, personal notes/feedback). Keep business metadata unmarked.
- The subject defaults to the `EventSourceId<T>` identity — set `[Subject]`/`ICanProvideSubject`/a tuple `Subject` only when the subject is a non-`EventSourceId<T>` value. A managed read-model document has one subject — don't mix multiple people's PII in one document.
- Bearer tokens, magic links, and signed URLs are not durable facts — store keyed hashes / opaque references, not the secret.

## Output shape

Write the brief in this order: **(1)** stream boundaries and subjects → **(2)** commands and events → **(3)** read models and consumers → **(4)** automations/translations → **(5)** compliance notes → **(6)** specs.

If a subject boundary or erasure behavior can't be made person-level without changing product behavior, **stop and surface that trade-off** before implementing.

## Slice lifecycle — Draft → Ready → Working → Done

A slice is the unit of work, and it moves through four states. The model (events, commands, read models, UI/screens, specs) lives inside the slice; **anyone — human or agent — can author or update it**, and the lifecycle doesn't care who made the change.

- **Draft** — being modeled; events/commands/read models/boundaries still in flux.
- **Ready** — the **handoff gate**: the model is *information-complete* (the checks above pass — every read-model field traces to an event, every event has a consumer; commands, authorization, compliance, and the specs outline all decided). A Ready slice can be implemented with **no further modeling decisions**.
- **Working** — an implementer (agent or human) has picked it up and is running the **Implementation Workflow** end-to-end: backend slice file → Debug+Release build → specs → frontend → docs → quality gates. Use the `new-vertical-slice` skill; keep `EventModel.md` in sync via `create-event-model`.
- **Done** — every quality gate is green and the change is shipped (PR merged, CI green — via `ship-changes`). Ready for downstream slices to build on.

**Marking a slice Ready is the signal to implement it** — the implementer runs the full workflow through to Done rather than stopping at the model, and doesn't batch Ready slices behind one another. (This is the Event Modeling "agent harness" loop: model → Ready → pick up → work → Done → repeat.) This stays subject to the [Collaboration Default](../../rules/general.md) — pause only for a genuine checkpoint, risky change, or a decision the model can't make.

## See also

- `create-event-model` — render the chosen model into a Mermaid `EventModel.md`.
- `new-vertical-slice` — implement a Ready slice end-to-end (the Working state).
- `ship-changes` — branch, commit, PR, and merge to reach Done.
- `vertical-slices.md` — slice anatomy that implements the brief.
