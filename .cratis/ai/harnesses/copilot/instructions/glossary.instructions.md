---
applyTo: "**/*"
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/glossary.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

# Cratis Glossary

One precise line per load-bearing term, so the same word means the same thing everywhere. When a rule or skill uses one of these, this is the definition it intends.

## Events & streams

- **Event** — an immutable, past-tense **fact** that something happened; an `[EventType]` record. Never mutated; lives in the log forever.
- **Event source** — the entity an event stream belongs to (the stream key); identified by an `EventSourceId<T>`.
- **Event source id** — the strongly-typed identity of an event source (`EventSourceId<T>`). Implicit in the event context — **never** an event payload property.
- **Event stream** — the ordered events for one event source within an event sequence.
- **Event sequence** — a named, append-ordered log; `EventSequenceId` (the event **log**, **outbox**, **inbox**).
- **Event log** — the default event sequence where domain events are appended.
- **Outbox / inbox** — cross-service event sequences: a producer appends its public contract event to the outbox; a consumer observes the inbox.
- **EventContext** — metadata traveling with an event (event source id, sequence number, occurred, causation/correlation, subject).

## Read side

- **Read model** — a `[ReadModel]` record holding queryable derived state, built by a projection or reducer; exposes `static` query methods.
- **Projection** — declares how a read model is built by consuming **events** (model-bound attributes, or fluent `IProjectionFor<T>`); pure events→state, no side effects, never reads other read models.
- **Reducer** — `IReducerFor<T>`; a "current state + event → next state" read-model builder; the last-resort escape hatch when projections can't express the transition.
- **Reactor** — `IReactor`; observes events and produces **side effects** (notifications, commands, follow-up events). The "if this then that".
- **Observer** — Chronicle's umbrella for anything consuming an event sequence (projection, reducer, reactor); carries subscription / replay / **quarantine** state. Projection replay and catch-up each walk the sequence in one globally ordered job step; a new catch-up failure stops that step. Full replay is inactive and completion does not resume a quarantined observer (Chronicle v19.50.1, `Documentation/concepts/observers.mdx`, `Observation/Jobs/{CatchUpObserver,ReplayObserver}.cs` under `Source/Kernel/Core/`).
- **Sink** — where a read model is persisted (MongoDB, EF Core).
- **AutoMap** — Chronicle's on-by-default mapping of matching event→read-model property names; **never call `.AutoMap()`**.
- **Query** — a `static` method on a `[ReadModel]`; returns derived state, **snapshot** (one-shot) or **observable** (live).

## Write side

- **Command** — a `[Command]` record expressing an imperative **intent**; its public `Handle()` produces event(s) (or a response).
- **Provide()** — the command method that fetches/computes data after validation/authorization and before `Handle()`; may short-circuit with a `ValidationResult`.
- **Causation chain** — the ordered links saying how an append came about (root process → command → …), each carrying properties. A command records its **name and its property values** there, so an event says what the command was asked to do; the chain lives in the event log and is as permanent as the events.
- **Constraint** — `IConstraint`; an **append-time** invariant (uniqueness / concurrency) enforced at the event-store level.
- **DCB (Dynamic Consistency Boundary)** — enforcing a state-dependent rule under concurrency over explicitly enrolled event-state reads: stream decisions/exact scopes or protected, admitted projection decision reads. Plain injected models and `Result<TEvent, ValidationResult>` alone are advisory, not a guard.
- **Consistency boundary** — the scope within which an invariant holds atomically (an event source, a routed stream, or the event state enrolled by an admitted protected decision read).
- **EventForEventSourceId** — self-describing wrapper to append an event to a **specific** (cross-stream) event source, from a command `Handle()` or from a reactor handler (reactor support has shipped since Chronicle 15.35). It carries the event stream type and id, source type, subject, occurred time, tags and causation, so it is also how a reactor sets those explicitly.
- **ReactorDelivery** — the identity of one delivery of one event to one reactor partition; declare it as a handler parameter and Chronicle passes it in. Stable across a replay and across recovering a failed partition, so a receipt kept under its `Id` is what makes a side effect survive re-delivery. An identity, not a guarantee — Chronicle does not know whether the effect ran.

## Structure & types

- **Slice** — the vertical unit of one behavior (command + events + projection + read model + component + specs), created/changed/deleted together.
- **Slice types** — **State Change** (command → events), **State View** (events → read model), **Automation** (events → side effect, including a follow-up command or event — a Screenplay `produces` / `invokes`), **Translation** (data from *outside* the model's own facts → our own facts: an external system's or another service's data recorded as events we own, keeping the external fact distinct from the local one). A reactor that turns our own event into a follow-up event or command is an Automation, not a Translation.
- **Concept** — a `ConceptAs<T>`, a strongly-typed wrapper over a primitive domain value (never raw `Guid`/`string`/`int`).
- **EventSourceId<T>** — the strongly-typed event-source identity; derive entity identities from this, not `ConceptAs<Guid>`.
- **Proxy** — the generated TypeScript command/query client, produced from C# on a **Debug** build; carries a `// @generated by Cratis` header and is never hand-edited.

## Compliance & multi-tenancy

- **Subject / `[Subject]`** — the natural person a piece of PII belongs to (for GDPR erasure); defaults to the `EventSourceId<T>` identity.
- **`[PII]`** — marks an inherently personal value so Chronicle can manage/erase it.
- **`[NotAudited]`** — an **Arc** marking (`Cratis.Arc.Chronicle.Commands`, not a Chronicle type) for a value that is secret but *not* personal data (password, token, API key), so it is never written to the causation chain. Withholds only; it does not encrypt the value, so pair it with `[Encrypted]` for an operational secret. It never enrolls a value in erasure the way Chronicle's `[PII]` does.
- **`[Encrypted]`** — a Chronicle marking (`Cratis.Chronicle.ProtectedValues`, Chronicle 19.32.0+) that encrypts an **operational secret** (API key, token, a company's bank account number) at rest on the event, without erasure. It does not keep a command input off the causation chain, so a secret gets `[Encrypted]` **plus** `[NotAudited]`. Never combine it with `[PII]` (`CHR0053`); never put it on an event source id (`CHR0052`).
- **`[OnceOnly]`** — marks a reactor handler (or the whole reactor class) as non-replayable: Chronicle skips it for every event arriving as part of a **replay** — observer rewind, redaction, revision. Replay-exclusion only, not exactly-once, not a per-event-source run limit or deduplication; ordinary catch-up, crash redelivery and failed-partition recovery can run the handler again (Chronicle v19.50.1, `Documentation/code-analysis/CHR0022.mdx`). Use a `ReactorDelivery` receipt for that case.
- **Namespace / tenant** — Chronicle isolates tenants by **namespace**; each namespace has its own events, observers, and read models.

## Profiles

- **Application profile** — building an app *on* Cratis (event-sourced CQRS, vertical slices, MVVM frontend).
- **Framework profile** — contributing to a Cratis framework repo *itself* (Arc, Chronicle, Fundamentals, Components — libraries). See `framework.md`.
