<!-- cratis-ai-managed: skills/cratis-chronicle-decision-reads/references/choosing-how-a-rule-holds.md -->
# Choosing how a write-side rule holds

## Contents

- Route first
- The matrix
- A plain read guards nothing
- Constraints
- Decision reads and protected decisions
- Arc traps
- Chronicle-only traps
- Costs and why protection is opt-in
- Checklist for generated code

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Shared by the Chronicle, Arc and Screenplay consistency skills. It answers one
question: **which mechanism makes this rule hold while other writers append at the
same time**, and which look correct but do not. Read it before writing a command,
validator, endpoint or handler that reads state and then appends.

Checked against the Chronicle and Arc sources. Published guides, which
this agrees with:
[Chronicle: choosing how a rule holds](https://www.cratis.io/chronicle/dynamic-consistency-boundary/choosing/),
[decision reads](https://www.cratis.io/chronicle/read-models/decision-reads/),
[Arc: choosing how a command's rule holds](https://www.cratis.io/arc/backend/csharp/chronicle/consistency/).

## Route first

Take the first step that matches the rule.

1. **A value must be unique, or something may exist once** (email, ISBN, one
   registration per source): a constraint. Not a read, not a validator.
2. **The rule uses data outside Chronicle events**: record the external fact as an
   event first, then decide on it; otherwise accept the risk and repair. No decision
   read covers external state.
3. **A rare violation is acceptable** (overbooking you can compensate): append
   normally and repair with a reactor that returns a correcting event.
4. **Someone edited what someone else changed** (the user saw revision N): an explicit
   concurrency scope built from the revision the user supplied.
5. **A rule over one key's history** (cannot ship before payment, stock or count
   on one key): a decision read of a flat, event-source-keyed projection.
6. **A rule over one stream inside a source**: a stream-scoped decision read
   (Chronicle) or `IStreamReads`/`StreamDecision` (Arc).
7. **A rule across two keys** (course capacity and a student's course limit): one
   decision read per key, one event per event source, one atomic `AppendMany`; or
   put every fact the rule needs on one event source and use one read.
8. **Rich behaviour over one lifecycle** (many rules, one key): a richer flat
   projection read as one decision read, or in Arc an aggregate root.
9. **Only a UX hint** (disable a button, show seats left): a plain read.
10. **Only input shape** (range, format, required): a validator. No guard needed.

## The matrix

| Rule shape | Use | Never use | Too much |
| --- | --- | --- | --- |
| Unique value | `[Unique]` property or `IConstraint` | read model check then append | decision read or scope (they guard one key's history) |
| Create once | `[Unique]` on the event type | "does it exist" read then append | decision read |
| One key's history, stock, count | decision read; Arc `[ProtectedDecision]` + `DecisionRead<T>` | plain read model, validator read, `concurrency: true` | aggregate for one counter |
| Two keys | two decision reads + one event per source in one `AppendMany` | two separate appends, a guard on one side only | none |
| One stream | stream-scoped decision read; Arc `IStreamReads` | whole-source read when one stream matters | none |
| Edit conflict | explicit scope from the user's revision | last writer wins by accident | constraint |
| Rich behaviour | richer projection read once; Arc aggregate | rules spread over validators and plain reads | aggregate for one rule |
| Rare violation acceptable | append, repair with a reactor | presenting a plain read as a guarantee | decision read |
| UX hint | plain read | treating the hint as enforcement | decision read (several calls and a full fold) |
| Hot key, long history | shorten histories (split by period or stream); constraint for uniqueness | ignoring cost: each decision read refolds the key | none |

Not a guard: a plain read, a `CommandValidator` that reads, `Result<,>`, returning
`ValidationResult.Error`, `concurrency: true`, the default optimistic append, a
reactor, or an `ExpectedRevision` you never pass into a scope.

## A plain read guards nothing

Whatever the backing, a plain read captures no position and enrolls no guard.

| Backing | Freshness | Guard |
| --- | --- | --- |
| Materialized projection or reducer (default) | may lag the log | none |
| `[Passive]` projection | fresh at read time | none |
| `[Passive]` reducer | fresh at read time | none |
| Self-reducing model | as its `[Passive]` setting | none |

Passive is fresher, not safer. Capturing the tail yourself before a materialized
read does not fix it: the read may be older than the position you captured, and
the events in between are neither seen nor guarded. Do not rebuild a decision read
by hand. Every Arc read-model injection (`Handle`, `Provide`, validator
constructor) and every `IReadModels.GetInstanceById` is a plain read, on a
protected command too.

## Constraints

A constraint binds every writer, whichever client or service appends, per namespace
and per event sequence. It cannot count, sum or test a predicate; it expresses
uniqueness (with release on a removal event), create-once and closed streams.

- The claim is settled after the durable append (not yet
  implemented in every store). A failed index update is logged and a later duplicate
  may pass until a reindex. SQL and InMemory stores do not yet settle claims
  A constraint is the right mechanism, not an
  unconditional proof.
- Tenants in different namespaces can claim the same value.
- `[RemoveConstraint]` releases attribute constraints (`[Unique]` on a property
  or on the event type); on a fluent `IConstraint` it compiles and does nothing,
  so use `.RemovedWith<T>()` there.
- It is not re-run when the kernel retries a duplicate sequence number.
- For a friendly message use `.WithMessage`; Arc maps a violation to HTTP 400.

## Decision reads and protected decisions

A decision read folds an admitted projection in a fresh session after capturing
the log boundary, and the append is rejected if an event of the model's types
for that key arrived after the boundary, **whoever wrote it** (the kernel checks the
stored tail filtered by key, types and route). See the skill's
[admission and ownership](admission-and-ownership.md) for the refusal list.

- **Admission.** A flat projection keyed by event source, with a finite event-type
  set and an `Id` property (string, or Guid in lowercase hyphenated form). Reducers,
  self-reducing models, joins, children, `FromEvery`, event-property routing and
  other sequences are refused. A model without `Id`, or keyed by a concept that does
  not convert, refuses with `KeyConversion`; in Arc that surfaces as HTTP 500.
- **Absent models are guarded.** A concurrent creation conflicts. Do not fabricate a
  default instance for an absent model.
- **Cross-key.** A read on key X guards X even when the command appends only to Y.
  Per-key reads cover only keys in the read set: when new keys can join the set
  ("a member holds at most three loans" with each loan on its own source), write the
  facts on a key that is read, usually the member's.
- **Every deciding writer must be guarded.** The guard detects every matching
  event appended after its boundary, but it protects only the decision that carries
  it. If the guarded write lands first, a writer that decides on the same facts
  without a guard (a plain read, an `[Unprotected]` command, a direct append) can
  still append on stale state and break the rule. The rule holds only when every
  writer that decides on it is guarded, or when it is a constraint, which checks
  every append.
- **Conflict.** No automatic retry. Reread and decide again, or resubmit. Revise/redact,
  other sequences, external state, projection definition changes and writes that
  bypass the kernel's serialized append path (duplicate grain activations, direct
  storage writes) are outside the guard.
- **Empty protected commit.** A unit that enrolled a decision read and staged no
  events appends an empty batch with the guard scopes. The kernel
  validators (`AppendManyValidator`, `AppendManyForEventSourcesValidator`) accept an
  empty batch when a scope carries an actual boundary or expects no matching event, and the integration
  spec `for_ReadModels/when_reading_for_a_decision` shows `AppendMany([], guardedBy:)`
  succeeds when nothing changed and reports a concurrency violation when the model
  changed. A kernel that rejects an empty batch ("At least one event is required.")
  surfaces as `DecisionReadValidateOnlyNotSupported`, which fails closed. Read from
  source, not run end to end: treat validate-only as supported on a current kernel
  and assert it with an integration specification before relying on it.
- **Arc opt-in.** Protection is per command: `[ProtectedDecision]` marks a command
  that takes `DecisionRead<T>` or command-aware `IDecisionReads`; unmarked commands
  keep legacy behaviour, `[Unprotected]` opts out. There is no app-wide option, because
  making it default would refuse validators with dependencies, refused shapes and
  commands appending through an injected `IEventLog`.
  The key is the command's event source id; for other keys use
  `IDecisionReads.Get<T>((ReadModelKey)id)`.

## Arc traps

| Trap | What happens | Fix |
| --- | --- | --- |
| Read model injected into `Handle`, `Provide` or a validator, then events returned | plain read; two requests both win | decision read, constraint or stream decision |
| `CommandValidator` that reads state | advisory; also refused on a protected command | validator for shape rules only |
| Protected command with a discoverable validator that has constructor dependencies or several public constructors | HTTP 500, nothing runs | parameterless validator (exactly one public parameterless constructor); move state rules into the decision |
| `concurrency: true` on `EventStreamId`, `EventStreamType` or `EventSourceType` | reads the narrowed tail after `Handle`; changes granularity, not the decision. A command that also reads `IReadModels` still triggers `ARCCHR0011` | decision read, explicit scope or stream decision |
| Default (no attributes) | Arc reads the tail when the unit of work commits, after `Handle` | only protects the window between that read and the write |
| First append into a scope | unchecked unless `ChronicleOptions.ConcurrencyOptions.CheckFirstAppendIntoAScope` is set | constraint, or an aggregate (new aggregates expect no first event) |
| Two or more key candidates (`EventSourceId`-typed, `[Key]`-marked or implicitly convertible) | `ARCCHR0002` warns, and `[Key]` does not silence it. At runtime Arc silently takes the first eligible property in declaration order (`EventSourceId`-typed or `[Key]`-marked, no priority for derived types; an unmarked `ConceptAs<Guid>` is not eligible). With one candidate Arc keys by it silently; with none each call gets a new id | implement `ICanProvideEventSourceId` |
| No id property | a random event source id per call; nothing can conflict | give the command a key |
| `[Unprotected]` command taking `DecisionRead<T>` | resolves to an unguarded read | use `[ProtectedDecision]` |
| Injected `IEventLog.Append` in `Handle` | immediate write that survives a conflict rollback | return events |
| Aggregate `Commit()` whose result is ignored, or `Failed()` without `Commit()` | staged events still commit | return `await aggregate.Commit()` |
| Aggregate with an enrolled decision read | `Commit()` throws `ProtectedUnitOfWorkRequiresOwner` | choose one mechanism per command |
| Aggregate for a rule across aggregates | each aggregate guards only itself | cross-key decision reads |
| Tests without `UseDecisionReads()` and a competing `AppendConcurrently` | the race is never exercised | `CommandScenario` with decision reads |

## Chronicle-only traps

- Decision read, then plain `IEventLog.Append`: the immediate append bypasses the
  guard. Stage with `Transactional.Append`, or pass `guardedBy:` to `AppendMany`.
- `Get<T>` needs an ambient unit of work. `GetDetached<T>` enrolls nothing until you
  pass it to `AppendMany(..., guardedBy:)` or `AddDecisionRead`.
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

## Costs and why protection is opt-in

A decision read makes about four calls per attempt (up to three attempts), folds the
key's whole history of the model's types on every read, shares a per-model-type
projection grain across keys and tenants, and is not retried on conflict. Use it
where histories are short and write contention per key is low; shorten histories
before reaching for it on a hot key. A constraint costs one index lookup inside the
append. Decision reads are .NET only.

## Checklist for generated code

- Name the mechanism and say why the rule shape needs it. A plain read, validator,
  `Result<,>` or `concurrency: true` is never presented as the guard.
- Uniqueness is a constraint. A state rule has a decision read, scope or stream
  decision enrolled **before** the events are staged.
- The decision-read model is admitted (flat projection, `Id`, event-source keyed).
- Cross-key rules append one event per source atomically.
- A specification interleaves a competing append and asserts the conflict.
- What stays unguarded (an unguarded writer's own decision, external state, revisions) is stated.
