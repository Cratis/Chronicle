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
| `Cratis.Chronicle` (client) | `16.45.2` | `[Unique]`, `[RemoveConstraint]`, `IConstraint`, `IConstraintBuilder`, `IUniqueConstraintBuilder`, `ConstraintViolation`, `IAppendResult` |
| `Cratis.Chronicle` code analysis | `16.45.2` | `CHR0017`, `CHR0018`, `CHR0020` |
| `Cratis.Chronicle.Testing` | `16.45.2` | `EventScenario` and the `IAppendResult` `Should*` assertions |

Reverify against the Chronicle repository before claiming support for another
version. Never translate an attribute argument order or an assertion name from
memory.

## Route near misses

- The rule rejects **command input** — a range, a format, a cross-field
  condition: use `cratis-arc-command-validation`.
- The rule is state-dependent and belongs in the handler under concurrency (the
  DCB pattern): also `cratis-arc-command-validation`.
- You are creating or changing a read model that the rule reads: use
  `cratis-chronicle-read-model`.
- You are writing the specification and want the `EventScenario` mechanics in
  depth: use `cratis-chronicle-event-specifications`.

## Step 1 — Pick the mechanism

| Situation | Use |
| --- | --- |
| One event of this type per event source | class-level `[Unique]` on the event type |
| A property value unique across event sources | property-level `[Unique]` on that property |
| Release a claimed value when something is removed | `[RemoveConstraint("name")]` on the removal event |
| The same value must be unique across **several** event types | `IConstraint` with one `[Unique(name: …)]` name, or a fluent `Unique(…)` |
| Case-insensitive matching, composite keys, or a scoped constraint | `IConstraint` |

**Per-event-source versus across-event-sources is the constraint *kind*, not a
setting.** Class-level `[Unique]` (and the generic `Unique<TEvent>()` builder
call) means "at most one such event per event source id". Property-level
`[Unique]` (and `Unique(unique => unique.On<T>(…))`) means "this value is unique
across event sources". Choosing the wrong one gives a constraint that never
fires rather than a compile error.

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
- Several removal events may declare the same name; **any one of them releases
  the claim on its own**.

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
and an `On(EventType, string[])` overload. `On<TEvent>` is **variadic** —
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

⚠️ A `[PII]` property's value is encrypted before the constraint index sees it,
so the claimed value and the violation details hold the encrypted form.
Uniqueness still works; a human reading the index will not see the plaintext.

## Step 6 — Specify the violation

Specify the constraint with `EventScenario`: seed the conflicting state through
`Given`, append again, and assert
`ShouldHaveConstraintViolationFor(<Module>ConstraintNames.UniqueX)` — **the
constraint name, never the message**. A message is presentation text and will
change without the behavior changing.

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

⚠️ **`EventScenario` disables concurrency checking.** It wires a no-op
concurrency-scope strategy, so `ShouldHaveConcurrencyViolations()` can never
become true through the in-process scenario. Constraint violations are fully
exercisable there; concurrency violations need the real kernel.

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
- No specification asserts on a violation message string.
- Build clean in Debug and Release, and the specifications pass.
