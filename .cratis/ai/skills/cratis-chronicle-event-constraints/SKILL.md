---
name: cratis-chronicle-event-constraints
description: Enforce append-time uniqueness in Chronicle with the [Unique] attribute, [RemoveConstraint], and IConstraint, and specify the violation with EventScenario. Use when a rule must hold at the event store rather than in a command, so two concurrent appends cannot both win. Do not use for ordinary command input validation and do not use to create a projection.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-event-constraints/SKILL.md -->

# Chronicle event constraints

A constraint is an **append-time** invariant. Chronicle evaluates it inside the
append, against the event store's own index — so it is the only mechanism that
holds when two requests race. A command-side pre-check reads state that may
already be stale by the time the append lands; a constraint cannot be.

Constraints are discovered automatically. There is no registration call.

## Verified product sources

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Chronicle` (client) | `19.50.1` | uniqueness, stream closing, constraint builders and append results |
| `Cratis.Chronicle` code analysis | `19.50.1` | `CHR0017`, `CHR0018`, `CHR0020` |
| `Cratis.Chronicle.Testing` | `19.50.1` | `EventScenario` and the `IAppendResult` `Should*` assertions |

Sources: `Source/Clients/DotNET/Events/Constraints/`,
`EventSequences/IEventSequence.cs` and `Source/Clients/Testing/` at v19.50.0.

Reverify against the Chronicle repository before claiming support for another
version. Never translate an attribute argument order or an assertion name from
memory.

## Route near misses

- The rule rejects **command input** — a range, a format, a cross-field
  condition: use `cratis-arc-command-validation`.
- The rule is state-dependent and needs concurrency protection: use enrolled
  stream/projection decision reads with `cratis-arc-command-validation`.
  Plain injected models and `Result<,>` alone remain advisory.
- You are creating or changing a read model that the rule reads: use
  `cratis-chronicle-read-model`.
- You are writing the specification and want the `EventScenario` mechanics in
  depth: use `cratis-chronicle-event-specifications`.
- An accepted `.play` model under the model root covers the behavior, or the
  repository is opted in (the root holds a committed `.play` file (`git ls-tree -r --name-only HEAD` lists a `.play` file there, narrowed to `-- <root>` when a root is configured), or the project set
  `mcpServers.screenplay.root` in `.cratis/ai.json`; an empty directory, install
  output, an uncommitted `.play` draft or a `.play` file outside the root does not count; master definition:
  `cratis-screenplay-modeling-lifecycle`): change the model first with
  `cratis-screenplay-event-modeling`. If the Screenplay skills are not installed,
  say so and do not author `.play` from memory.
  Edit code here only for infrastructure, clients, adapters, Screenplay code
  attachments, or gap-fill scope (`cratis-screenplay-render-and-gap-fill`);
  never edit Stage-managed output.

## Step 1 — Pick the mechanism

| Situation | Use |
| --- | --- |
| One event of this type per event source | class-level `[Unique]` on the event type |
| A property value unique across event sources | property-level `[Unique]` on that property |
| Release a claimed value when something is removed | `[RemoveConstraint("name")]` on the removal event |
| The same value must be unique across **several** event types | `IConstraint` with one `[Unique(name: …)]` name, or a fluent `Unique(…)` |
| Case-insensitive matching, composite keys, or a scoped constraint | `IConstraint` |

Class-level `[Unique]` (and `Unique<TEvent>()`) means "at most one such event
per event source id". Property-level `[Unique]` (and `Unique(unique => unique.On<T>(…))`)
means "this value is unique across event sources". `Mode` tunes **property-value
retention**, not that distinction: `PerEventSource` (default) releases the old
value when the source claims a new one; `PerValue` keeps every value reserved
until a removal releases it. Reclaiming one's own value succeeds in either mode.

## Step 2 — `[Unique]` — the default

```csharp
// Event-type uniqueness — only one of this event per event source
[EventType]
[Unique(message: "A project with this name already exists.")]
public record ProjectRegistered(ProjectName Name);

// Property uniqueness — the value must be unique across event sources
[EventType]
public record UserRegistered([Unique(name: "UniqueEmail", message: "Email already registered.")] EmailAddress Email, DisplayName Name);

// Release a claimed value on removal — the name must match the [Unique] name exactly
[EventType]
[RemoveConstraint("UniqueEmail")]
public record UserRemoved;
```

- `UniqueAttribute`'s constructor is `(string? name = default, string? message = default)` —
  **name first, message second**. Pass them by name, as above, so a future
  reader cannot misread the order.
- The attribute targets `Class | Property`, `AllowMultiple = false`.
- **The name defaults to the member or type name** when omitted. Give it an
  explicit name whenever a removal event or another event type has to refer to
  it, because the string in `[RemoveConstraint]` must match exactly.
- `RemoveConstraintAttribute` is class-only with `AllowMultiple = true`. Stack
  several on one removal event to release several claimed values at once.
- To enforce the same property across **multiple** event types, give each its
  `[Unique(name: "UniqueEmail")]` with the **same name**.
- Several removal events may declare the same name; **any one releases the claim**.
- `Mode = UniqueConstraintMode.PerValue` retains all values. A removal can use
  `Properties = [nameof(RemovalEvent.Value)]` to release one value (composite
  properties in constraint order); empty `Properties` releases all values held
  by the source in the scope.
- `EventSequences = [EventSequenceId.LogId]` limits validation/indexing to the
  event log. Empty means every sequence; when declarations share a name, their
  sequence sets combine and an unrestricted declaration wins.
- Null/missing property values do not claim `""`; clearing a default-mode value
  releases its claim. `IgnoreCasing` is honored by the kernel.

## Step 3 — `IConstraint` — the advanced form

Reach for `IConstraint` when uniqueness spans event types with different
property names, needs `.IgnoreCasing()`, covers a composite of properties, or a
`RemovedWith` event must release it.

```csharp
public class UniqueProjectName : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(unique =>
            unique
                .On<ProjectRegistered>(e => e.Name)
                .IgnoreCasing()                    // case-insensitive — do NOT lowercase inside the lambda
                .RemovedWith<ProjectRemoved>());   // omit if there is no remove event
}
```

`IUniqueConstraintBuilder` also carries `WithName(name)`, `WithMessage(message)`,
`WithMode(UniqueConstraintMode)`, and an `On(EventType, string[])` overload. `On<TEvent>` is **variadic** —
`On<T>(e => e.Tenant, e => e.Email)` constrains the composite of both
properties, not two independent values.

`IConstraintBuilder` additionally offers `PerEventSourceType()`,
`PerEventStreamType()`, and `PerEventStreamId()` to narrow the scope a
uniqueness claim is keyed by, and a `Unique<TEvent>(message, name)` overload for
event-type uniqueness. **That overload takes the message first and the name
second** — the reverse of the `[Unique]` attribute. Both parameters convert
implicitly from `string`, so the wrong order compiles and silently swaps them.
Always pass them by name.

`ConstraintScope` is a record of three optional dimensions, not an enum, and you
never construct it — the three `Per…` calls select which dimensions participate.
`ForEventSequences(...)` / `ForEventLog()` select the sequences to validate and
index. Keep these separate from scope dimensions.

## Step 4 — Keep `Define` declarative

`Define` builds a rule; it never runs one. Three analyzers enforce that, all at
error severity:

| Id | Rejects |
| --- | --- |
| `CHR0017` | Injecting `IEventLog` or `ICommandPipeline` into a constraint |
| `CHR0018` | An imperative statement in `Define` — `if`, loops, `switch`, `return`, `throw`, a local declaration, an assignment |
| `CHR0020` | An expression lambda that is not a pure member-access chain |

`CHR0020` is the one that bites: `e => e.Email.ToLower()` is a method call, so
it is rejected. Use `.IgnoreCasing()` instead. Suppressing the analyzer does not
make it work — the runtime path extracts an **empty** property path from a
method call and then fails at startup with `PropertyDoesNotExistOnEventType`,
which is a much harder failure to read.

## Step 5 — Treat a violation as a result, never as an exception

A violated constraint comes back on the append result, not as a throw. On
`IAppendResult`: `IsSuccess`, `HasConstraintViolations`,
`HasConcurrencyViolations`, `HasErrors`, `ConstraintViolations`, and `Errors`.
Each `ConstraintViolation` carries `ConstraintName`, `ConstraintType`,
`Message`, `Details`, the `EventTypeId` and the `SequenceNumber`. Arc surfaces
it as a validation error on the command result.

Note the asymmetry: `AppendResult` exposes a singular nullable
`ConcurrencyViolation`, while `AppendManyResult` exposes a plural
`ConcurrencyViolations`. Only the `HasConcurrencyViolations` flag is on the
interface.

⚠️ Constraints are evaluated and indexed against the **plaintext** of a `[PII]`
property, not its ciphertext — PII encryption is non-deterministic (a fresh key
and nonce per value), so hashing the encrypted form would never detect a
collision. The kernel builds a plaintext copy for the constraint check and
persists only the encrypted event; on reindex it releases (decrypts) stored
content to recompute the hashes, and skips subjects whose key has been erased so
they do not all collide on the hash of an empty value. The index therefore holds
plaintext-derived hashes, not the personal value itself.

## Step 6 — Specify the violation

Specify the constraint with `EventScenario`: seed the conflicting state through
`Given`, append again, and assert
`ShouldHaveConstraintViolationFor(<Module>ConstraintNames.UniqueX)` — **the
constraint name by default**. A message is presentation text and will change
without the behavior changing. Assert exact message text only when that wording
is the specified behavior, and name that behavior in the fact.

The example compiles into the application assembly, so it uses `#if DEBUG`.
Dedicated spec projects need no wrapper.

```csharp
#if DEBUG
public class and_the_name_already_exists : Specification
{
    EventScenario _scenario;
    IAppendResult _result;

    async Task Establish()
    {
        _scenario = new EventScenario();
        await _scenario.Given
            .ForEventSource(ProjectId.New())
            .Events(new ProjectRegistered("Acme"));
    }

    async Task Because() =>
        _result = await _scenario.EventLog.Append(ProjectId.New(), new ProjectRegistered("Acme"));

    [Fact] void should_be_failed() => _result.ShouldBeFailed();
    [Fact] void should_have_constraint_violation_for_unique_name() =>
        _result.ShouldHaveConstraintViolationFor(ProjectConstraintNames.UniqueProjectName);
}
#endif
```

Use per-test unique values (`$"{Guid.NewGuid():N}"`) so specifications are not
order-dependent. For a release, append the removal event and then assert the
value can be claimed again.

⚠️ **The default `EventScenario` client scope strategy answers `ConcurrencyScope.None`.**
A scenario-only concurrency assertion without a checked scope is vacuous.
The scenario runs the kernel grain/validator, but explicit-scope violations
have not been proven end to end here; use real-client integration for that proof.
Constraints are fully exercisable. Dispose the scenario in `Destroy()`.

## Close a stream at its business end

Choose the lifetime with **cratis-chronicle-event-modeling**, then enforce it at
append time. `IEventSequence.CompleteStream(ClosedStreamScope,
expectedTailSequenceNumber?)` returns `Result<EventSequenceNumber, CompleteStreamError>`:
`EmptyScope`, `AlreadyCompleted`, or `ExpectedTailMismatch` are not successful
new closures. An append matching a persisted closed scope is refused with
`StreamClosed`. Unspecified dimensions are wildcards, so name the intended
source/type/stream dimensions precisely.

Prefer an event-owned declaration when closing is a business fact:
`[ClosesStream]` / `IConstraintBuilder.ClosesStreamOn<TEvent>()`. The attribute
has `Dimensions`, `EventStreamIdFrom`, `ReopenedBy`, `Name` and `EventSequences`;
default dimensions are source id, stream type and stream id. Closing happens in
the **same grain turn** as append, **not crash-atomically** with it. Registering
the declaration is not retroactive; reindex rebuilds event-owned closures.
Reopening events remove only their constraint's exact closure, never a manual
operator closure.

Read [closing scope and recovery](references/closing-streams.md) when implementing
"close the books", choosing a scope, specifying the refusal, or repairing a
mistaken manual close. `IsStreamCompleted` and `GetClosedStreams` inspect;
Workbench's **Closed streams** page is read-only. Kernel `ReopenStreamScope`
requires an authorized operator and records `StreamScopeReopened` before
removing a manual closure. Closing does not erase history or prevent replay.

## What breaks

- **The constraint never fires.** You used class-level `[Unique]` for a value
  that has to be unique across event sources, or property-level `[Unique]` for
  a "one per event source" rule. Re-read Step 1 — the kind is the switch.
- **`[RemoveConstraint]` releases nothing.** The string does not match the
  `[Unique]` name. When `[Unique]` had no explicit name the real name is the
  member or type name, which is easy to get wrong; name it explicitly.
- **`PropertyDoesNotExistOnEventType` at startup.** A non-member-access lambda
  reached the builder — the `CHR0020` case with the analyzer suppressed.
- **`MissingNameForUniqueConstraint` / `NoEventTypesAddedToUniqueConstraint`.**
  The fluent builder was built without a name, or without any `On` call.
- **`NoUniqueEventTypeConstraintToRemove`.** `IConstraintBuilder.RemovedWith<T>()`
  was called with no preceding `Unique<TEvent>()`. That method attaches to the
  last event-type constraint; the fluent one on `IUniqueConstraintBuilder` is a
  different method.

## Verify

- Every constraint is discovered without a registration call.
- `[Unique]` arguments are passed by name, and every name a removal event refers
  to is explicit rather than defaulted.
- `Define` contains no imperative statement, no injected effect dependency, and
  no lambda that is not a pure member-access chain — `CHR0017`, `CHR0018` and
  `CHR0020` are all clean.
- Every constraint has an `EventScenario` specification that seeds the conflict
  and asserts `ShouldHaveConstraintViolationFor(<name>)` on the name.
- Violation message strings are not asserted by default. Exact wording is
  asserted only when it is the specified behavior, named in the fact.
- Build clean in Debug and Release, and the specifications pass.
