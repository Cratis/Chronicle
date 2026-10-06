---
name: cratis-arc-command-operation
description: Declare immediate inline work as a Cratis Arc ICommandOperation returned by a model-bound command, with optional compensation and commit-aware recovery. Use when moving service writes out of Handle(), choosing operation return shapes, diagnosing recovery, or specifying execution and compensation through CommandScenario. Do not use for durable after-commit delivery, validation-only changes, or framework return-type extensions through response value handlers.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-command-operation/SKILL.md -->

# Return work from an Arc command

A command should be able to decide which seat to reserve without reserving it
in the decision spec. `Handle()` returns immutable descriptions of the work;
Arc executes them and, when the commit boundary permits, compensates attempted
work. You write the business reversal, not a rollback stack.

**Command operations are the recommended way to express immediate inline side
effects in model-bound commands.** Direct service calls remain supported.
Chronicle is not required.

## Verified product sources

| Package / source assembly | Verified version | Purpose |
| --- | --- | --- |
| `Cratis.Arc.Core` | `22.48.1` | `Cratis.Arc.Commands`: operation declarations, pipeline, scopes, recovery observations |
| `Cratis.Arc.Core.CodeAnalysis` / `Cratis.Arc.Core.Generators` | `22.48.1` | ARC0016–ARC0018 and typed invocation; generator ships inside Core, not as a standalone package |
| `Cratis.Arc.ProxyGenerator` | `22.48.1` | Excludes operations and batches from the client response |
| `Cratis.Arc.Testing` | `22.48.1` | `Cratis.Arc.Testing.Commands.CommandScenario<TCommand>` and operation assertions |
| `Cratis.Arc.Chronicle` / `Cratis.Arc.Chronicle.Testing` | `22.48.1` | Deferred event commitment and real in-process constraint-rejection specs |

Verified at **Arc v22.48.1**, against the
[operation docs](https://github.com/Cratis/Arc/blob/v22.48.1/Documentation/backend/csharp/commands/operations/index.md),
[execution reference](https://github.com/Cratis/Arc/blob/v22.48.1/Documentation/backend/csharp/commands/operations/reference.md),
`Source/DotNET/Arc.Core/Commands/`, the analyzer/generator, and the testing
sources. Reverify for other versions. These are C# APIs, not the separate
Arc for TypeScript server API.

## Choose the boundary

| Need | Use |
| --- | --- |
| Acquire data for the decision | `Provide()` or explicitly supplied decision inputs |
| Reject input or a business decision | Validators or a recognized `ValidationResult` alternative |
| Return information | An ordinary response |
| Perform immediate external work chosen by the command | `ICommandOperation` |
| Reverse attempted work after an eligible failure | Optional `Compensate()` |
| Record durable Chronicle facts | Returned events, enrolled for deferred commitment |
| Reliable work after commit: email, payments, retriable webhooks | A reactor, outbox, or durable workflow; **not inline operations** |
| Use an external result to make the decision or response | An explicit service call; operations cannot return receipts |
| Extend framework interpretation of a specialized return type | Response value handlers, not application operation orchestration |

Do not disguise a write as `Provide()`. Supply identities, clock values, and
state before `Handle()`; returning operations does not make hidden I/O pure.

## Declare the work and its reversal

Use an immutable record implementing `ICommandOperation`. Business data belongs
in properties; services belong in method parameters. This example reuses the
application-owned `ReservationId`, `SeatId`, and `ISeatReservations` from Arc's
[reservation example](https://github.com/Cratis/Arc/blob/v22.48.1/Documentation/backend/csharp/commands/operations/implementing.md).
The provider's `Reserve` and `Cancel` methods return `Task`; they are not Arc APIs.

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;

namespace SeatBooking;

public sealed record ReserveSeat(ReservationId ReservationId, SeatId SeatId)
    : ICommandOperation
{
    public Task Execute(ISeatReservations reservations, CancellationToken cancellationToken) =>
        reservations.Reserve(ReservationId, SeatId, cancellationToken);

    public Task Compensate(ISeatReservations reservations, CancellationToken cancellationToken) =>
        reservations.Cancel(ReservationId, cancellationToken);
}

[Command]
public record BookSeat(ReservationId ReservationId, SeatId SeatId)
{
    public (ReservationId Response, ReserveSeat Operation) Handle() =>
        (ReservationId, new ReserveSeat(ReservationId, SeatId));
}
```

Register the application provider through the host's normal DI. No separate
operation-handler or response-value-handler registration is needed.

- Exactly one public, non-generic instance `Execute`; at most one optional
  public, non-generic instance `Compensate`.
- Return only `void`, `Task`, or `ValueTask`. No values, `Task<T>`, or
  `ValueTask<T>`: neither method supplies more pipeline values or a receipt.
- Method-parameter DI resolves required services from the originating command's
  provider. Each method may request at most one `CancellationToken`.
- At most one `CommandOperationFailure` parameter, **only on `Compensate()`**.
- No `async void`, static/generic methods, overloads, optional/`params`/by-ref
  arguments, pointers, or service-locator parameters. Do not capture services.
- `ARC0016` diagnoses invalid methods; `ARC0017` rejects bare operation
  collections; `ARC0018` requires accessible, non-file-local, non-generic
  public/internal operation types and accessible non-generic containing types.

Omit `Compensate()` when there is no meaningful reversal; Arc does not invent
one. Full contract: [execution and recovery](references/execution-and-recovery.md).

## Return one, none, or many

| `Handle()` return | Meaning |
| --- | --- |
| Concrete operation / `ICommandOperation` | One server-executed operation |
| Nullable operation | Null is absent and skipped |
| `CommandOperations` | Explicit ordered batch; `[]` and `default(CommandOperations)` are empty |
| Tuple | Operations, other server-handled values, and at most one client response |
| `Result` / `OneOf` | Only the active branch, including a tuple alternative |
| `Task<T>` / `ValueTask<T>` | Await first, then classify the supported inner shape |

This handler fragment assumes `First`/`Second` reservation IDs and
`FirstSeat`/`SecondSeat` seat IDs are command inputs:

```csharp
public CommandOperations Handle() =>
[
    new ReserveSeat(First, FirstSeat),
    new ReserveSeat(Second, SecondSeat)
];
```

**Never return a raw array or `IEnumerable<ICommandOperation>` as a batch.**
`CommandOperations` materializes membership once and rejects null elements.
Ordinary collections are response data, not an implicit operation executor.
**Keep the signature statically operation-bearing.** Arc decides whether a
command participates from `Handle()`'s *declared* return type, and unwraps only
tuples and `Result`/`OneOf` branches. So:

- An operation returned as a top-level value of an `object`-typed `Handle()`
  (alone or inside a tuple) is rejected with `InvalidCommandOperation`.
- **An operation placed inside `IEnumerable<object>` or `object[]` — the usual
  multi-event shape — is silently not executed.** Neither ARC0017 nor the
  runtime sees it; it is treated as ordinary response data. To return events and
  operations together, use a tuple such as
  `(IEnumerable<object> Events, CommandOperations Operations)`.
- A controller action that returns an operation object does not execute it;
  operations run only through the model-bound command pipeline.

Operations never reach the client/proxy; only the ordinary response does.
**Calling `Handle()` directly does not execute operations.** Use `ICommandPipeline`
or `CommandScenario` for orchestration; `Validate()` does not run operations.

## Execution and compensation

Arc preflights operation metadata and dependencies, processes control values and
other server values (including returned-event enrollment), then executes
operations sequentially. A failure stops later operations. Required scopes
complete before Arc selects recovery from observed commitment facts.

If A and B return, C throws inside `Execute()`, and D never starts, eligible
compensation runs **C, B, A — never D**. The throwing operation is included:
a lost acknowledgment may hide a successful external write. A dependency
preflight failure is not an entered invocation.

Compensators must be ownership-aware and safe for partially applied or lost-ack
work. A delete-if-present can race a create still in flight. The provider owns
idempotency, tenant/owner isolation, conflicting-key rejection, terminal
cancellation or reconciliation, and protection of earlier finalized successes
from a later attempt's reversal. Arc cannot supply those guarantees.

## Commit facts decide recovery

On command failure with started operations:

| `CommandCommitDisposition` | Action |
| --- | --- |
| `NoCommit` / `NotCommitted` | Attempt declared compensation in reverse order |
| `Committed` | Suppress reversal associated with committed business facts |
| `Unknown` / `Mixed` | Report indeterminate recovery; do not guess or reverse |

**A failed result is not proof that nothing committed.** Compensation is new
business work, not atomic rollback. It is best-effort, in-process, with no durable
journal and no automatic retries of execution or compensation. Crashes,
post-pipeline serialization, and result delivery are outside this recovery.

With Chronicle, **return events for deferred enrollment**. Operations run before
automatic transaction completion. A known rejection can permit compensation;
an uncertain commit cannot. Do not explicitly commit aggregates or immediately
append events and expect operation recovery to undo them: if the commit
participant has already committed (or its outcome is unknown or mixed) when
operations would start, Arc rejects the batch with `InvalidCommandOperation` and
none of them run. Explicit commits made inside an operation-capable command are
refused outright.

`Execute()` receives the command token. `Compensate()` receives a separate token;
`CommandOperationOptions.CompensationTimeout` defaults to **30 seconds**, shared
by the recovery attempt. The budget is cooperative: code ignoring cancellation
is still awaited, not forcibly terminated.

## Keep the execution boundary supported

The supported profile is **flat, sequential, with at most one deferred commit
participant**. **No same-host nested commands at all** while an operation-capable
command runs: an `ICommandPipeline` call from its `Provide()`, `Handle()`,
validators, `Execute()` or `Compensate()` — or any nested call whose child
command is itself operation-capable — is refused and the child never runs.
During execution this rejects the batch even if the child result is ignored;
during compensation it is recorded as a failed compensation. Compose operation
declarations instead, or move follow-up commands to a reactor.

**Every discovered `ICommandExecutionScope` must implement
`ICommandOperationExecutionScope`.** "Operation-capable" means `Handle()`'s
declared return type structurally contains `ICommandOperation` or
`CommandOperations` — directly, nullable, in a tuple, `Task`/`ValueTask`, or
`Result`/`OneOf`. For such a command Arc rejects execution with
`InvalidCommandOperation` before any scope begins if any scope is unclassified — even when the handler returns null or
an empty batch. Audit the application's own unit-of-work or audit scopes before
adopting operations.

Custom scopes opt in through `ICommandOperationExecutionScope`; they must report
real commit facts, not infer them from `IsSuccess`. A nonparticipant promises not
to commit business changes; `Begin()` must not commit. Chronicle supplies its
integration. **EF Core and MongoDB have no built-in operation commit participant.**
See the [custom-scope checklist](references/execution-and-recovery.md#supported-scopes).

## Observe and test on the backend

Backend `CommandResult.Recovery` and `OperationOutcomes` describe recovery and
entered invocations. Both are excluded from HTTP JSON and the TypeScript result.
`Completed` recovery means callbacks returned, not that external history was
erased or a retry is safe. Keep operation types and failure details in trusted
diagnostics.

Test three boundaries with `Cratis.Specifications`:

1. Direct `Handle()`: assert immutable decision data and order, without services.
2. Direct `Execute()` / `Compensate()`: assert provider requests with substitutes.
3. `CommandScenario`: assert real execution, original failure, skipped operations,
   compensation, and commit observations. Register controlled dependencies first.

See [testing](references/testing.md) for failure, Chronicle rejection, and
migration. Provider tests must separately prove ownership and idempotency.

## Common mistakes

| Mistake | Correction |
| --- | --- |
| Calling the service in `Handle()` and returning its operation | Remove the direct write; otherwise it runs twice |
| Calling `Execute()` manually before returning the operation | Let the pipeline invoke it once |
| Moving the write into `Provide()` | Acquire inputs there; writes remain outside operation recovery |
| `try/catch` rollback stacks in `Handle()` | Return work and implement its business `Compensate()` |
| Raw `IEnumerable<ICommandOperation>` | Return `CommandOperations` (`ARC0017`) |
| Capturing services in the record | Inject them into operation methods |
| Generating an operation's request/ownership key (or clock values it depends on) inside `Handle()` | Have the caller supply it so a retry reuses the same key |
| Using inline operations for email/payment delivery that must survive a crash | Use reactors/outbox/durable workflows |
| Assuming `!IsSuccess` means nothing committed | Inspect backend commitment and recovery observations |
| Marking a committing scope as a nonparticipant | Report its actual boundary; do not evade the single-participant limit |

## Route near misses

- Command definition / `Provide()`: `cratis-arc-command`.
- Rejection rules: `cratis-arc-command-validation`.
- Calling an existing command: `cratis-arc-command-execution`.
- Reliable reactions to committed facts: `cratis-chronicle-reactor`.
- Specialized framework return-value interpretation: Arc's
  [response value handlers](https://github.com/Cratis/Arc/blob/v22.48.1/Documentation/backend/csharp/commands/response-value-handlers.md).

## Verify

- The work belongs inline; no durable delivery requirement was lost.
- Inputs and caller response are preserved; `Handle()` only declares the work.
- Method shapes satisfy ARC0016–ARC0018; batches use `CommandOperations`.
- The provider protects ownership, earlier successes, partial work, and retries.
- Every registered execution scope implements `ICommandOperationExecutionScope`,
  honors the flat boundary, and reports authoritative commit facts.
- Decision, adapter, and scenario specs cover success and a specific failure;
  Chronicle specs prove real non-commit, not only a fabricated failed result.
- Debug and Release builds are clean; the generated proxy contains no operation
  descriptor and the client still compiles against the intended response.
