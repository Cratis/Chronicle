<!-- cratis-ai-managed: skills/cratis-chronicle-decision-reads/references/choosing-how-a-rule-holds.md -->
# Choosing how a write-side rule holds

## Contents

- Defaults
- Route by rule shape
- The matrix
- Reads under Off, Measure and On
- Constraints
- What a read guard covers
- Refusals
- Arc traps
- Chronicle-only traps
- Roll out with Measure
- Test the race
- Costs
- Checklist for generated code

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Shared by the Chronicle, Arc and Screenplay consistency skills. It answers one
question: **which mechanism makes this rule hold while other writers append at the
same time**, and which look correct but do not. Read it before writing a command,
validator, endpoint or handler that reads state and then appends.

Published guides this agrees with:
[Chronicle: choosing how a rule holds](https://www.cratis.io/chronicle/dynamic-consistency-boundary/choosing/),
[decision reads](https://www.cratis.io/chronicle/read-models/decision-reads/),
[Arc: choosing how a command's rule holds](https://www.cratis.io/arc/backend/csharp/chronicle/consistency/).

## Defaults

- Read protection is `Off` unless the project sets it. New code starts with
  `On`: `<CratisReadProtection>On</CratisReadProtection>` in the project (it
  emits `[assembly: ReadProtection(ReadProtectionMode.On)]`; an invalid value
  fails the build). Existing code moves `Off` to `Measure` to `On`; see
  [Roll out with Measure](#roll-out-with-measure).
- Under `On`, a read model that is a parameter of `Handle()` or of a
  `CommandValidator<T>` constructor is read fresh from the event log and guards
  the command's append. The state rule goes in the validator with the read model
  as a constructor parameter; `Handle()` returns the events.
- `[ProtectedDecision]` with `DecisionRead<T>` is the older per-command opt-in. It
  still works and is not obsolete. Use it only in a project that is not on `On`.
  It is not redundant with `On`; do not remove it because `On` is set without
  checking what the command takes.
- Subject ids derive from `EventSourceId` (`record Isbn(string Value) :
  EventSourceId(Value)`), so the command needs no `[Key]`.

## Route by rule shape

Take the first step that matches. Names S1 to S14 label the shapes used in the
published scenario catalogue.

1. **A value is unique, or something exists once** (S2: email, ISBN, one card per
   member): a constraint. Not a read, not a validator.
2. **The rule uses data outside Chronicle events** (S11): record the external fact
   as an event first, then decide on it; otherwise accept the risk and repair.
3. **A rare violation is acceptable, a hint, or a hot key** (S11): no guard.
   Append and repair with a reactor that returns a correcting event, or mark a
   deliberate advisory read `[Unprotected("why")]`.
4. **Someone edited what someone else changed** (S10: the user saw revision N): an
   explicit concurrency scope built from the revision the client sent.
5. **One entity's state** (S1: last copy, balance, count): the read model as a
   parameter under `On`, the rule in a validator. Not an aggregate for one counter.
6. **Two entities** (S3 loan limit and last copy; S9 course capacity and student
   limit): two read-model parameters, the second chosen by convention or
   `[LookUpBy(nameof(Command.Property))]`, and `Handle()` returns
   `IEnumerable<EventForEventSourceId>` with one event per entity in one atomic
   append. Use distinct event types per entity.
7. **A limit inside one stream of one source** (S4: 5000 per account per month): a
   per-stream read model, `[ReadModel, EventStream<TSource>("Transactions")]`, with
   the command routed `[EventSource<TSource>("Transactions")]` and
   `[EventStreamId("{Month}")]`. For a short stream and a question about raw events
   (S5: approve a period once), a stream decision with `IStreamReads`.
8. **Process stream with steps and completion** (S6): a definition with a stream per
   process, declared concurrency and a closed stream.
9. **Gapless numbers** (S7): a numbering source read with `IDecisionReads` in
   `Provide()`; `[Unique]` alone gives no duplicates but allows gaps.
10. **Rich behaviour over one lifecycle** (S8: order lines, pay, ship, cancel): an
    aggregate root, returning `Commit()` last. An aggregate and a protected read
    of the same source in one command are refused.
11. **Moving an existing app to `On`** (S12): `Measure` first.
12. **A command failed under protection** (S13): the [refusals](#refusals).
13. **Prove it** (S14): [test the race](#test-the-race).

## The matrix

| Rule shape | Use | Never use | Too much |
| --- | --- | --- | --- |
| S2 Unique value, create once | `[Unique]` property or event type, `IConstraint` | "exists?" read then append | read model or decision read (they guard one key's history) |
| S1 One entity's state | read-model parameter + validator under `On` | validator over a plain read under `Off` (a hint) | aggregate for one counter |
| S3, S9 Two entities | two parameters, `[LookUpBy]` for the second key, `EventForEventSourceId` per key | a guard on one side only, two separate appends, one read model keyed through an event property | none |
| S4 Limit inside one stream | per-stream read model on a routed command | whole-source model when another stream's events must not conflict | none |
| S5 Once per period in a short stream | stream decision (`IStreamReads`) or per-stream model | whole-source read | none |
| S8 Rich lifecycle | aggregate root | aggregate plus a read of the same source | aggregate for one rule |
| S10 Edit conflict | explicit scope from the client's revision | `concurrency: true` alone (reads the tail after `Handle`) | constraint |
| S11 Rare violation, hint, external data | repair reactor, `[Unprotected("why")]`, or record the external fact first | presenting a plain read as a guarantee | any guard |
| S12 Existing app | `Measure`, then `On` per project | switching a whole app to `On` unmeasured | none |
| Hot key, long history | shorten histories (split by period or stream); constraint for uniqueness | ignoring cost: each guarded read folds the key's history | none |

Not a guard: a plain read under `Off` or `Measure`, a validator over such a read,
`Result<,>`, `ValidationResult.Error`, `concurrency: true`, the default optimistic
append, a reactor, or an `ExpectedRevision` never passed into a scope.

## Reads under Off, Measure and On

| Mode | Read-model parameter, validator constructor | Results and status codes |
| --- | --- | --- |
| `Off` (default) | plain read; a hint, guards nothing | as today |
| `Measure` | plain read; Arc reports what `On` would do | same as `Off` |
| `On` | read fresh from the log; guards the command's append | a lost race is HTTP 400 |

A command overrides the project with `[ReadProtection(ReadProtectionMode.On)]`
(`Cratis.Arc.Commands`); command-level `[Unprotected]` means `Off`. The runtime
cap `ReadProtection:MaximumMode` only lowers (a missing or invalid value is `On`,
no cap); a lowered command logs a startup Warning. With the cap set, the `On`
behaviour does not hold.

A plain read does not capture a position even when the model is `[Passive]`:
passive is fresher, not safer. Do not rebuild a guard by hand (capturing the tail
yourself before a read): the read may be older than the position you captured.

Under `On`:

- `IReadModels`, `IMongoCollection` and detached reads in the command are refused.
  Take the model as a parameter, or mark the method `[Unprotected("why")]` (it
  covers the body, not injected parameters).
- Immediate appends (an injected `IEventLog.Append`) are refused. Return the events.
- The key is, in order: `[LookUpBy(nameof(Cmd.Prop))]` on the parameter
  (parameter-only attribute, `Cratis.Arc.Commands`); the single command property
  whose type equals the model's `Id` type; `AmbiguousReadModelLookup` (HTTP 500) if
  several match; the command's event source id.
- Admitted: event-source-keyed model-bound or declarative projections with a
  string or Guid `Id`, and unfiltered reducers without constructor dependencies
  (except `ILogger<T>`; self-reducing models are exempt). Refused with
  `ReadCannotBeProtected` unless `[Unprotected("why")]`: joins and children, a
  model keyed by an event property, Pinned generation delivery, filtered reducers,
  reducers with dependencies, other event sequences.
- A validator's read models are protected when the validator is convention
  registered (transient). Hand-registered scoped, singleton, factory or instance
  validators are refused.
- Under `[ProtectedDecision]` without `On`, only a parameterless validator runs.

Caller severity: model-bound commands accept `X-Allowed-Severity` (`allowedSeverity`).
Under `Off` and `Measure` a caller sending `Error` removes Error results from validators,
command filters and `Provide()`, so `Handle()` runs, unless the command declares
`[BlockOnValidationSeverity(ValidationResultSeverity.Error)]`. Requires Arc 22.108.0:
a command declared `On` gets an `Error` floor (`Error` and `Unknown` always block; the
caller can relax only `Warning` and `Information`). Lowering
`ReadProtection:MaximumMode` at runtime does not remove it, and an explicit attribute
wins. Before that version a validator's state rule can be skipped by the caller even
under `On`.

## Constraints

A constraint binds every writer, whichever client or service appends, per namespace
and per event sequence. It cannot count, sum or test a predicate; it expresses
uniqueness (with release on a removal event), create-once and closed streams.

- The claim is settled after the durable append (not yet implemented in every
  store). A failed index update is logged and a later duplicate may pass until a
  reindex. SQL and InMemory stores do not yet settle claims. A constraint is the
  right mechanism, not an unconditional proof.
- Tenants in different namespaces can claim the same value.
- `[RemoveConstraint]` releases attribute constraints (`[Unique]` on a property or
  on the event type); on a fluent `IConstraint` it compiles and does nothing, so
  use `.RemovedWith<T>()` there.
- It is not re-run when the kernel retries a duplicate sequence number.
- For a friendly message use `.WithMessage`; Arc maps a violation to HTTP 400.

## What a read guard covers

Under `On` (and for a Chronicle decision read), if an event of the model's types
for that key arrives after the read, nothing from the command is written, whoever
wrote the event. The kernel checks the stored tail filtered by key, types and route.

- **Only that key and those types.** A concurrent event of another type (a rename)
  does not conflict.
- **Absent models are guarded.** A concurrent creation conflicts. Do not fabricate
  a default instance for an absent model.
- **Cross-key.** A read on key X guards X even when the command appends only to Y.
  Per-key reads cover only keys in the read set; when new keys can join the set
  ("a member holds at most three loans" with each loan on its own source), write
  the facts on a key that is read, usually the member's.
- **Every deciding writer must be guarded.** The guard protects only the decision
  that carries it. A writer that decides on the same facts through a plain read, an
  `[Unprotected]` read or a direct append can still append on stale state. The rule
  holds only when every writer that decides on it is guarded, or when it is a
  constraint.
- **Outside the guard:** revisions and redactions (append conditions do not detect
  them), services and external stores, other event sequences, `[Unprotected]`
  reads, `Off` mode, projection definition changes, and writes that bypass the
  kernel's serialized append path (duplicate grain activations, direct storage
  writes).
- **Conflict.** Arc and Chronicle do not re-run the command; the fold of a read
  may retry up to three times. The client reads fresh state and decides again.
- **Aggregates.** Under `On`, an aggregate and a protected read of the same source
  are refused (`ReadAndAggregateOnSameSource`); a read of a different source is
  fine. Arc guards an aggregate's commit with a scope from its rehydration position.
- **Empty protected commit.** A unit that enrolled a read and staged no events
  appends an empty batch with the guard scopes. Current kernels validate it and
  write nothing; a kernel that rejects an empty batch surfaces
  `DecisionReadValidateOnlyNotSupported`, which fails closed. Assert it in an
  integration specification before relying on it.
- **Chronicle model.** Chronicle supports decision models and append conditions
  within its event-source model: event source id, event source type, event stream
  type and stream id play the role of tags, one value each per event. It is not a
  DCB event store: an event belongs to one event source, and a fact about two
  entities is two events in one atomic append. Custom tags take no part in
  consistency.

## Refusals

| Seen by the caller | Cause | Do |
| --- | --- | --- |
| HTTP 400, `ValidationResult` reason `concurrencyViolation`, `reasonDetail` `ReadChanged`, `state` `{ readModel, key }` | an event of the read model's types for that key arrived after the read; nothing was written | resubmit; the client reads fresh state and decides again |
| HTTP 400, `ReadIncomplete` | the fold did not finish after three attempts | resubmit |
| HTTP 500 | the command cannot be guarded: a code or configuration error | change the code; retrying does not help |

The `ReadChanged` message reads "BookStock 'isbn-1' changed after BorrowBook read
it. Nothing from this command was written; submit it again." It does not name the
conflicting event. Outside Development, a 500 message is replaced unless
`ArcOptions.ExposeExceptionDetails` is set. Frontend (TypeScript):
`CommandResult.isSuccess/isValid/validationResults`, `ValidationResult.reason`
(`'concurrencyViolation'`, `ValidationResultReason` from `@cratis/arc/validation`),
`reasonDetail` (a string with no TS constant), `state`.

Common 500 refusals: `ReadCannotBeProtected` (shape, above),
`AmbiguousReadModelLookup` (add `[LookUpBy]`), `ImmediateAppendWhileReadsAreProtected`
(return the event), `ReadRefusedWhileReadsAreProtected` and
`DetachedReadWhileReadsAreProtected` (parameter, `IDecisionReads.Get` in
`Provide()`, or `[Unprotected("why")]`), `ReadAfterProtectedCommit` (read first,
return `Commit()` last), `ReadOfSourceWithStagedEvents`, `ExplicitScopeDoesNotCoverReads`,
`DeclaredRoutingNotCoveredByExplicitScope`, `ConflictingReadProtection`,
`ReadModelStreamNotOnCommandRoute`.

## Arc traps

| Trap | What happens | Fix |
| --- | --- | --- |
| Read model parameter or validator under `Off` or `Measure`, then events returned | plain read; two requests both win | set `On`, or use a constraint or stream decision |
| Validator over a plain read under `Off` | a hint with a friendly message; guards nothing | `On` |
| `concurrency: true` on `EventStreamId`, `EventStreamType` or `EventSourceType` | reads the narrowed tail after `Handle`; changes granularity, not the decision | `On`, explicit scope or stream decision |
| Default (no attributes) | Arc reads the tail when the unit of work commits, after `Handle` | only protects the window between that read and the write |
| First append into a scope | unchecked unless `ChronicleOptions.ConcurrencyOptions.CheckFirstAppendIntoAScope` is set | constraint, or an aggregate |
| A `UsingKey(e => e.Member)` projection read | refused (`ReadCannotBeProtected`) | append a fact to the entity the model is keyed by |
| Two or more key candidates (`EventSourceId`-typed, `[Key]`-marked or implicitly convertible) | `ARCCHR0002` warns, and `[Key]` does not silence it; Arc takes the first eligible property in declaration order | implement `ICanProvideEventSourceId` |
| Per-stream model on a command without a route | `ReadModelStreamNotOnCommandRoute` (500, analyzer `ARCCHR0037`) | add `[EventSource<TSource>("Stream")]` and `[EventStreamId("{Prop}")]` |
| Whole-source read beside a per-stream read of the same source | both drop the stream dimensions (Measure reports `coarsened`) | keep one granularity |
| No id property | a random event source id per call; nothing can conflict | give the command a key |
| Injected `IEventLog.Append` in `Handle` | immediate write that survives a conflict rollback | return events |
| Aggregate `Commit()` whose result is ignored, or `Failed()` without `Commit()` | staged events still commit | return `await aggregate.Commit()` |
| `[Unprotected]` command taking `DecisionRead<T>` | resolves to an unguarded read | remove `[Unprotected]` |
| Tests without `AppendConcurrently`, or a scenario left in legacy mode | the race is never exercised | [test the race](#test-the-race) |

## Chronicle-only traps

- Decision read, then plain `IEventLog.Append`: the immediate append bypasses the
  guard. Stage with `Transactional.Append`, or pass `guardedBy:` to `AppendMany`.
- `Get<T>` needs an ambient unit of work. `GetDetached<T>` enrolls nothing until you
  pass it to `AppendMany(..., guardedBy:)` or `AddDecisionRead`. A unit can also
  call `EnforceReadProtection()` and `AllowReads(ReadAllowance.Advisory|Contributed,
  reason)`; `GetAppendConditions()` returns the `AppendCondition` values
  (`Cratis.Chronicle.Transactions`).
- Stream reads: `IReadModels.GetInstanceById<T>(EventSourceId, EventStreamId)` throws
  `StreamScopeNotSupported` unless the implementation supports it (analyzer CHR0064).
- The ASP.NET Core middleware commits **after** the response. Call
  `IUnitOfWorkCompletionFeature.CommitAsync()` before responding and check
  `IsSuccess` and `GetDecisionConflicts()`; the middleware reports no concurrency
  violations, so a late commit failure never reaches the caller.
- Ignoring the `AppendResult` loses a violation silently: violations are results.
- An explicit scope and a decision read on the same source in one unit throw
  `DecisionReadConflictsWithConcurrencyScope`; pick one.
- A manual scope built without a sequence number is skipped by the kernel; check
  `ConcurrencyCheckPerformed`. Capture the tail before reading the facts.
- Arc has a static class also named `ConcurrencyScopeBuilder`; in an Arc project
  qualify Chronicle's builder.

## Roll out with Measure

Steps: update the packages, set `<CratisReadProtection>Measure</CratisReadProtection>`,
run the application and its tests, read the report, fix or annotate each finding,
then set `On` per project. Behaviour for callers does not change under `Measure`.

- The report: meter `Cratis.Arc.ReadProtection`, counter
  `cratis.arc.read_protection.events` (tags `command`, `category`, `model`,
  `reason`), span event `read-protection.plan`, and a Warning once per command and
  category for `refused_read`, `immediate_append` and `scope_refusal`.
- Categories: `protected_read`, `refused_read` (reason names the cause),
  `advisory_read`, `unknown_dependency`, `redirect` (add `[LookUpBy]`),
  `immediate_append` (return the event), `conflict_today`, `self_write`,
  `coarsened`, `displacement`, `scope_refusal`, `matching_events_since_start`
  (someone else's matching event arrived mid-command: `On` might have returned
  400, a real race), `measurement_unavailable`, `mode_lowered`.
- Measure is best effort: one extra awaited call per command plus sampled
  background scans. A report is a set of hypotheses.
- Fixes: reducer with dependencies or filters, join or children, event-property
  key, Pinned delivery or `IMongoCollection`: change the read as the refusal says,
  or annotate `[Unprotected("why")]`. Under `On`, `ARCCHR0011` no longer applies;
  fix `ARCCHR0024`, `0028` and `0033`.

## Test the race

`new CommandScenario<T>()` selects the in-process decision log from the command's
read-protection mode and dependencies (for example under `On`); do not call
`UseDecisionReads()` (it is hidden and unnecessary). An `Off` command with no
protected dependency stays in legacy mode with pinned read models, which cannot
prove a race. Seed with
`scenario.Given.ForEventSource(id).Events(...)` (for a routed stream
`.OnRoute(route).Events(...)`). Guarded state needs events: protected reads fold the
event log and ignore a pinned model, and `DecisionRead<T>` parameters reject one.
`.ReadModel(m)` only pins an advisory (plain) read. Then queue a competing event with
`scenario.AppendConcurrently(id, events...)` and execute:

```csharp
await using var scenario = new CommandScenario<BorrowBook>();
scenario.Given.ForEventSource(isbn).Events(new BookAdded(Copies: 1));
scenario.AppendConcurrently(isbn, new BookBorrowed(other));
var result = await scenario.Execute(new BorrowBook(isbn, me));
result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
```

There is no `scenario.AppendConditions`. A pure decision test builds the command
and the validator with a hand-made read model. Chronicle-direct code asserts
`unitOfWork.GetAppendConditions()`. A default `EventScenario` scope strategy
returning `None` is not concurrency evidence.

## Costs

A guarded read makes about four calls per attempt (up to three attempts), folds
the key's whole history of the model's types on every read, shares a per-model-type
projection grain across keys and tenants, and is not retried on conflict. Use it
where histories are short and write contention per key is low; shorten histories
before reaching for it on a hot key. A constraint costs one index lookup inside the
append. Decision reads exist in the .NET and Elixir clients.

## Checklist for generated code

- Name the mechanism and say why the rule shape needs it. A plain read, a
  validator under `Off`, `Result<,>` or `concurrency: true` is never presented as
  the guard.
- New code sets `On`. Uniqueness is a constraint. A state rule is a read-model
  parameter in a validator, a per-stream model, a stream decision, an aggregate or
  an explicit scope, enrolled **before** the events are staged.
- The model is admitted (source-keyed projection or admitted reducer, `Id`).
- Cross-key rules append one event per source atomically, with distinct event types.
- A specification interleaves a competing append and asserts
  `ConcurrencyViolation`.
- What stays unguarded (an unguarded writer's own decision, external state,
  revisions, `[Unprotected]` reads, `Off` mode) is stated.
