---
name: cratis-arc-command-execution
description: Execute an existing Cratis Arc command from backend code through ICommandPipeline — the scopeless and scoped overloads, the typed Execute<TResult>, the pre-flight Validate, validation severity filtering, and how to read CommandResult. Use when a reactor, background job, scheduled task, or backend service must run a command without an HTTP request. Do not use to define a command.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-command-execution/SKILL.md -->

# Execute an Arc command from code

`ICommandPipeline` is the same entry point the HTTP boundary uses. Everything
downstream of the entry point is identical: authorization filters, validation
filters, argument resolution including `Provide()`, `Handle()`, and response
dispatch. `Execute` also runs returned command operations and completes eligible
recovery; calling `Handle()` directly only returns their declarations.

## Verified product sources

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Arc.Core` | `22.16.0` | `Cratis.Arc.Commands.ICommandPipeline`, `CommandResult`, `ValidationResultSeverity` |
| `Cratis.Arc.Chronicle` | `22.16.0` | analyzer `ARCCHR0006` for reactor replay |

> Re-verified at the versions above by **symbol and signature**: every type, attribute and member this skill names exists at that tag, and the public surface it describes is unchanged since the previous verification (Chronicle 16.45.x / Arc 22.10.4 — the Chronicle 16→18 client diff is converters, options and doc comments; no type was removed or renamed). Behavior claims were verified at the earlier tag unless a section says otherwise.

Reverify before claiming support for another version. Operation execution,
nesting restrictions, and backend recovery members below are verified at
**Arc v22.48.1**; see `cratis-arc-command-operation` for sources and limits.

## When this is the wrong tool

`ICommandPipeline` is for **command-shaped** work that needs authorization and
validation. It is not the way to append an event to another stream from inside a
handler — return an `EventForEventSourceId` from `Handle()` instead, and never
inject `IEventLog` (`ARCCHR0007`).

Do not call `ICommandPipeline` for a same-host nested command while an
operation-capable command runs (from its `Provide()`, `Handle()`, validators,
`Execute()` or `Compensate()`), nor execute an operation-capable command nested
inside another command: the flat operation boundary refuses it and the child
never runs. Ignoring
the child's failed result still rejects the executing batch; during compensation
it records a failed compensator. Compose operation declarations instead.

## Inject and execute

> **Reactors have a declarative alternative.** With the Arc Chronicle integration a
> reactor handler can **return** the `[Command]` (or a collection of commands) and
> Arc executes it; a denied or invalid result fails the partition instead of being
> discarded, and `[ExecuteCommandsAsSystem]` on the reactor class supplies a
> principal for those returned commands. That attribute does **not** cover manual
> `Execute` calls — the form below needs its own execution context. See the
> **cratis-chronicle-reactor** skill.

```csharp
public class <Name>Reactor(ICommandPipeline pipeline) : IReactor
{
    [OnceOnly]
    public Task <Something>Happened(<Something>Happened @event, EventContext context) =>
        pipeline.Execute(new <TargetCommand>(@event.<Id>, @event.<Value>));
}
```

`Execute(command)` is the scopeless overload: the pipeline creates its own DI
scope for the call. This is the normal case. **A DI scope is not an independent
transaction boundary**; see the nested-command contract below.

## The overloads

| Overload | Use it when |
| --- | --- |
| `Execute(object command, ValidationResultSeverity? allowedSeverity = default)` | The normal case |
| `Execute(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default)` | The command's dependencies must come from the caller's existing scope |
| `Execute<TResult>(object command, ...)` | The handler returns a response you need typed |
| `Validate(object command, ...)` | You need the verdict without any side effect |

Each has both the scopeless and the scoped form. Prefer the scopeless one: the
scoped overload exists for a caller that genuinely owns a scope the command's
dependencies must share.

```csharp
var result = await pipeline.Execute<<ResponseType>>(new <CommandName>(<args>));
if (result.IsSuccess)
{
    var response = result.Response;
}
```

⚠️ `Execute<TResult>` throws `InvalidCastException` when the handler returned a
response of a different type. When the handler returns no response, or the
command failed for any reason, `Response` is `default` — a failed result never
carries a response, because the pipeline clears it once the execution scopes
have completed.

## Nested commands and atomic batches

With **Arc's Chronicle integration**, an ordinary nested pipeline command joins
the outer command's ambient transaction. Only the outer owner commits pending
returned events. The child's successful result means enrollment, not persistence;
a commit-time constraint rejection can reject the entire batch. Inspect and
propagate child failures, and never report per-item persisted success before the
outer result is known.

This remains true with the scopeless overload or a newly created DI scope:
transaction context follows the async flow, not DI scope identity. Do not invent
a detached `Execute` overload or use fire-and-forget work to escape the owner.
Standalone Arc does not supply this Chronicle transaction guarantee. The flat
operation-capable boundary described above is stricter and rejects nesting.

Choose the intended outcome before implementing a bulk action:

- **All-or-nothing:** one command owns the returned event batch. Report its final
  result as one atomic outcome, not a list of independently committed children.
- **Independent outcomes:** orchestrate separate top-level pipeline executions
  outside an enclosing command transaction, awaiting each final result. A durable
  workflow/reactor can own follow-up work and retries. A collection of returned
  reactor commands executes as separate transactions; earlier successes remain
  when a later command fails, so retries must be idempotent.

Neither choice makes external service writes atomic. Immediate event appends
and explicit early commits also do not become rollbackable merely because a
command later fails; do not use them as an undocumented batch-isolation trick.

Checked against **Arc v22.49.0**
([transactional commands](https://github.com/Cratis/Arc/blob/v22.49.0/Documentation/backend/csharp/chronicle/commands/transactional-commands.md),
[transaction scope](https://github.com/Cratis/Arc/blob/v22.49.0/Source/DotNET/Chronicle/Commands/TransactionalCommandScope.cs),
[ambient context](https://github.com/Cratis/Arc/blob/v22.49.0/Source/DotNET/Chronicle/Commands/CommandTransaction.cs)).
Specify a later child's rejection and the actual persisted events: none for an
atomic returned batch, only completed independent items for separate commands.

## Pre-flight `Validate`

`await pipeline.Validate(command)` runs the authorization and validation filters
and stops there — `Provide()` and `Handle()` never run, so there is no side
effect. Use it to decide whether to commit to work that surrounds the command.

## Validation severity filtering

Both `Execute` and `Validate` take an optional
`ValidationResultSeverity? allowedSeverity` from `Cratis.Arc.Validation`. The
levels are `Unknown = 0`, `Information = 1`, `Warning = 2`, `Error = 3`.

- With no `allowedSeverity`, **only `Error` blocks**, and information and
  warnings are filtered off the result entirely — they never reach the caller.
- With an `allowedSeverity`, anything **strictly greater** blocks. Passing
  `Warning` lets warnings through onto the result while errors still block.

## Read the result by the specific flag

`IsSuccess` is derived — `IsAuthorized && IsValid && !HasExceptions`. Asserting
only on it cannot distinguish a forbidden caller from a rejected input from a
crash.

| Member | Meaning |
| --- | --- |
| `IsAuthorized` | The identity satisfied the command's authorization; `AuthorizationFailureReason` says why not |
| `IsValid` | Derived: no blocking validation results survived. `ValidationResults` carries them |
| `HasExceptions` | Derived: `ExceptionMessages` is non-empty. `ExceptionStackTrace` has the detail |
| `CorrelationId` | The pipeline-wide id to correlate logs and traces on |
| `Recovery` | Backend-only nullable commit/recovery summary when operations participate |
| `OperationOutcomes` | Backend-only observations of entered operations and their compensation |

`Recovery` and `OperationOutcomes` are excluded from HTTP JSON and the TypeScript
result contract. **A failed result is not proof that nothing committed.** Known
commit suppresses automatic reversal; unknown/mixed commitment is indeterminate,
not permission to compensate or retry. Consult `cratis-arc-command-operation`
before interpreting these observations.

A command with no registered handler comes back with an exception message naming
the command type rather than throwing.

Surface these through the result. Do not convert `!IsValid` into a thrown
exception — that discards the structured failure the caller needs.

## Reactors must decide what replay does

A reactor handler that executes a command is not idempotent: a replay
(redaction, revision, observer rewind) runs the handler again and executes the
command again. `ARCCHR0006` warns when a reactor handler invokes
`ICommandPipeline.Execute` without saying what replay should do. Mark the
handler `[OnceOnly]` so it is skipped during replay.

⚠️ `[OnceOnly]` is **replay exclusion, not deduplication**. There is no ledger
of events a handler has already seen: recovering a failed partition re-delivers
the event as an ordinary observation, and a `[OnceOnly]` handler runs again.
Make the executed command idempotent regardless — and when replay exclusion is
the wrong tool, suppress the diagnostic with a justification rather than
mis-marking it to silence the warning.

## Common mistakes

| Mistake | Why it hurts |
| --- | --- |
| Throwing on `!result.IsValid` | Loses `ValidationResults`, turns a 400-shaped rejection into a fault |
| Executing from a reactor without deciding about replay | Every replay re-fires the command |
| Reaching for the scoped overload by default | Couples the command to the caller's scope for no reason |
| Asserting only `IsSuccess` in a spec | A spec passes with every rule neutered |
| Reading `Response` without checking success first | It is always `default` on failure |

## Verify

- The caller branches on `IsAuthorized`, `IsValid` and `HasExceptions`, not only
  on `IsSuccess`.
- No expected failure is converted into a thrown exception.
- A reactor that executes a command has an explicit replay decision.
- The scoped overload is used only where the shared scope is required.
- `Execute<TResult>`'s type argument matches what the handler actually returns.
- `dotnet build` is clean in Debug and Release with `ARCCHR0006` and
  `ARCCHR0007` silent or explicitly justified.

## Route near misses

- Defining the command or choosing its `Handle()` return shape:
  `cratis-arc-command`.
- Declaring operations or diagnosing their execution/recovery:
  `cratis-arc-command-operation`.
- Adding or changing a rule: the Arc command validation guidance.
- Designing what a reactor should observe: the Chronicle reactor guidance.
- An accepted `.play` model under the model root covers the behavior, or the
  repository is opted in (the root holds a committed `.play` file (`git ls-tree -r --name-only HEAD -- <root>` lists it), or the project set
  `mcpServers.screenplay.root` in `.cratis/ai.json`; an empty directory, install
  output, an uncommitted `.play` draft or a `.play` file outside the root does not count; master definition:
  `cratis-screenplay-modeling-lifecycle`): change the model first with
  `cratis-screenplay-event-modeling`. If the Screenplay skills are not installed,
  say so and do not author `.play` from memory.
  Edit code here only for infrastructure, clients, adapters, Screenplay code
  attachments, or gap-fill scope (`cratis-screenplay-render-and-gap-fill`);
  never edit Stage-managed output.
