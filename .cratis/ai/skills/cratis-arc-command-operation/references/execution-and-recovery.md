<!-- cratis-ai-managed: skills/cratis-arc-command-operation/references/execution-and-recovery.md -->
# Operation execution and recovery reference

Verified at **Arc v22.48.1** against the
[product reference](https://github.com/Cratis/Arc/blob/v22.48.1/Documentation/backend/csharp/commands/operations/reference.md)
and `Source/DotNET/Arc.Core/Commands/`. Types below are in
`Cratis.Arc.Commands`. Start with [the skill](../SKILL.md) to choose the boundary.

## Declaration contract

| Surface | Contract |
| --- | --- |
| `ICommandOperation` | Marker; immutable business-data record is the convention, not a record-only runtime requirement |
| `Execute(...)` | Exactly one public, non-generic, non-abstract instance method |
| `Compensate(...)` | Optional single public, non-generic, non-abstract instance method |
| Method return | Only `void`, `Task`, `ValueTask`; no receipt or further pipeline values |
| Services | Required method parameters resolved from the command's provider; no service captures |
| `CancellationToken` | At most one per method, supplied by Arc |
| `CommandOperationFailure` | At most one, only on `Compensate` |
| Accessibility | Non-file-local, non-generic public/internal operation in accessible non-generic containing types |

No `async void`, static/generic methods, overloads, optional/`params`/by-ref or
pointer parameters. Service locators (`IServiceProvider`, `IServiceScopeFactory`,
`IServiceScope`, including implementing types) are forbidden parameters.
`ARC0016` checks method conventions, `ARC0017` checks bare operation collections,
and `ARC0018` checks accessibility for generated invokers. The generator emits
typed calls; validated reflection fallback covers source-free declarations.
This is not a claim of whole-host NativeAOT support.

## Returned values

| Return shape | Treatment |
| --- | --- |
| Concrete operation or `ICommandOperation` | Execute on the server |
| Nullable operation, including nullable value type | Null is absent; present value remains server-only |
| `CommandOperations` | Ordered immutable batch membership, materialized once; null elements rejected |
| `[]` / `default(CommandOperations)` | Empty batch |
| Nullable batch | Present value is server-only; prefer an empty non-nullable batch |
| Tuple | Flatten values; at most one ordinary client response |
| `Result` / `OneOf` | Flatten only the active alternative, including tuple alternatives |
| `Task<T>` / `ValueTask<T>` | Await the handler, then classify its result |
| Ordinary array/enumerable | Ordinary response classification, not an operation batch |

Bare collections of operations are rejected; use `CommandOperations`.
Participation is decided from the declared return type, and only tuples and
`Result`/`OneOf` are unwrapped: a top-level operation under an `object`
signature is rejected, while an operation inside `IEnumerable<object>`/`object[]`
is treated as response data and silently not executed. Combine events and
operations with a tuple, e.g. `(IEnumerable<object>, CommandOperations)`. Operations bypass
ordinary response handlers and never become client/proxy response types.

## Nine-step ordering

1. Run authorization, validation, `Provide()`, and `Handle()`.
2. Classify the return graph; validate operation metadata and preflight both
   execution and compensation dependencies before starting work.
3. Process control values before other server values, regardless of tuple order;
   enroll returned Chronicle events before operation execution.
4. If still successful and the commit boundary permits entry, execute operations
   sequentially in declaration order.
5. Record each invocation immediately before entering `Execute()`, after
   dependency resolution and cancellation checks.
6. Stop starting operations after execution failure or cancellation.
7. Complete required execution scopes in reverse scope order and obtain
   conservative commitment facts, including when completion failed.
8. Run eligible compensators in reverse invocation order; a failed compensator
   does not erase the original failure or prevent remaining eligible callbacks.
9. Publish backend observations and clear the result response if unsuccessful.

The throwing operation **is started** and eligible for compensation. Never-entered
operations are not. Dependency preflight is not entry. Direct calls to `Handle`,
`Execute`, or `Compensate` are ordinary C# calls without this orchestration.

## Commit and recovery

Failure plus started work requires a recovery decision; `!IsSuccess` does not
establish non-commit.

| `CommandCommitDisposition` | Meaning | Recovery on failure |
| --- | --- | --- |
| `NoCommit` | No coordinated participant | Attempt declared compensation |
| `NotCommitted` | Coordinated changes known not committed | Attempt declared compensation |
| `Committed` | Known committed business boundary | `Suppressed` |
| `Unknown` | Commitment cannot be safely established | `Indeterminate`; no reversal |
| `Mixed` | Some changes committed, others did not | `Indeterminate`; no blanket reversal |

With Chronicle, returned events enroll before operations; automatic transaction
completion follows them. A known commit rejection permits recovery. An uncertain
acknowledgment does not. Pending events still undergo real transaction rollback
on command failure; compensation does not un-append events. Explicit aggregate
commits are not supported within operation boundaries, and already successful
immediate appends cannot be undone. Operations cannot start after an observed
early, unknown, or mixed commit.

Recovery is best-effort, in-process **new business work**, not atomic rollback.
There is no durable journal, automatic retry, or promised crash recovery.
Serialization/result delivery after the pipeline and arbitrary service writes
outside returned operations are not covered.

## Backend observations

`CommandResult.Recovery` is nullable; processing that never participates can leave
it absent. `OperationOutcomes` is an immutable list of entered invocations. Both
carry `[JsonIgnore]`: neither is part of HTTP JSON or the TypeScript result.

| `CommandRecoverySummary` member | Meaning |
| --- | --- |
| `CommitDisposition` | Observed coordinated commitment, not command success |
| `Status` | `CommandRecoveryStatus` decision/outcome |
| `StartedCount` | Entered `Execute` invocations |
| `CompletedCount` | `Execute` methods that returned successfully |
| `CompensatedCount` | `Compensate` methods that returned successfully |
| `FailedCompensationCount` | Compensators that threw |
| `UncompensatedCount` | Started work requiring recovery without observed completed compensation |

| `CommandRecoveryStatus` | Meaning |
| --- | --- |
| `NotNeeded` | No started work requires recovery |
| `Completed` | Every required compensator returned; not proof of atomic reversal |
| `Incomplete` | Compensation unavailable, failed, or budget exhausted |
| `Suppressed` | Business changes committed |
| `Indeterminate` | Commitment unknown or mixed |

Each `CommandOperationOutcome` exposes `InvocationIndex`, `OperationType`,
`ExecutionCompleted`, `Compensation`, and optional `CompensationFailure` message.
It carries no operation business properties.

| `CommandOperationCompensation` | Meaning |
| --- | --- |
| `NotNeeded` | Recovery not required |
| `Completed` | Compensator returned |
| `Failed` | Compensator threw or attempted unsupported nesting |
| `NotAvailable` | No compensator declared |
| `BudgetExpired` | Recovery budget expired before callback entry |
| `Suppressed` | Commit facts prohibit automatic reversal |

Keep these observations and messages in trusted diagnostics, correlated with
`CommandResult.CorrelationId`. Do not treat `Completed` as permission to retry.

## Optional failure context

`CommandOperationFailure` is immutable context for specialized compensators,
not a mutable command result or an external execution receipt.

| Member | Meaning |
| --- | --- |
| `InvocationIndex` | Zero-based invocation index |
| `InvocationCompleted` | Its `Execute()` returned successfully |
| `IsFailingInvocation` | This invocation threw the original execution failure |
| `Source` | `CommandOperationFailureSource`: `Planning`, `ResponseHandling`, `Execution`, `Cancellation`, `ScopeCompletion` |
| `CommitDisposition` | Facts used to select recovery |
| `ExceptionMessages` | Defensive snapshot of original exception messages; recovery failures are separate |

A false `InvocationCompleted` does not prove the provider made no change. Do not
skip necessary reversal solely because forward execution threw.

## Cancellation and budget

`Execute` receives the command token (normally request-aborted over HTTP).
`Compensate` receives an independent recovery token so disconnection does not
immediately cancel cleanup. `CommandOperationOptions.CompensationTimeout` is
shared across the recovery attempt, defaults to 30 seconds, and must be positive
and at most 4294967294 milliseconds.

Host configuration fragment, with `System`, `Cratis.Arc.Commands`, and
`Microsoft.Extensions.DependencyInjection` imported:

```csharp
builder.Services.Configure<CommandOperationOptions>(options =>
    options.CompensationTimeout = TimeSpan.FromSeconds(10));
```

The budget is cooperative. A callback ignoring cancellation is still awaited;
Arc neither terminates it nor disposes its services underneath it. Not-yet-entered
callbacks are reported `BudgetExpired` after expiry. No automatic execution or
compensation retries occur; provider/driver policies can still retry internally.

## Supported scopes

Flat, sequential execution; at most one deferred commit participant. No
parallel/detached participation or durable recovery. Any same-host nested
`ICommandPipeline` call is refused (the child never runs) when the parent or the
child is operation-capable — from `Provide`, `Handle`, validators, `Execute` or
`Compensate`. Explicit commits inside the boundary are refused too. From
`Execute` a nested call rejects the batch even when its failed result is ignored. From `Compensate` it fails that compensation;
remaining eligible compensators still run.

Custom scopes implement `ICommandOperationExecutionScope`, extending
`ICommandExecutionScope`. Verify against the actual provider:

- `Begin(CommandContext)` does not commit; partial initialization is safe to clean up.
- `IsCommitParticipant` is stable and names responsibility, not the latest outcome.
- A nonparticipant truly does not commit business changes.
- `GetCommitDisposition(CommandContext)` is read **before operations start** as
  well as after completion. While the deferred commit is still pending it must
  report `NotCommitted` (or `NoCommit`); report `Committed`/`Unknown`/`Mixed`
  only when an early or uncertain commit actually happened, otherwise Arc
  refuses to start operations.
- It reports authoritative facts after `Complete` fails too; unavailable facts
  mean `Unknown`, not `NotCommitted`.
- Known commit remains committed after a later scope fails. Scope completion
  order is unchanged; never infer disposition from `IsSuccess` or a completed flag.
- Compensation can use its dependencies after completion; a live DI scope does
  not repair a failed transaction/context. Preserve tenant and ownership context.
- Cancellation/disposal cannot invalidate resources under running recovery.

| Integration | Boundary |
| --- | --- |
| Standalone Arc | Registered application services; no automatic database transaction |
| Chronicle | Compatible deferred event commit with conservative commitment observations |
| EF Core | No built-in operation commit participant; registration does not call `SaveChanges` or coordinate operation writes |
| MongoDB | No built-in operation commit participant; sessions and driver resilience remain provider concerns |

Independent EF and Chronicle commits do not become atomic by sharing a command.
Do not label a committing scope nonparticipating to evade validation.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| `Handle()` returns work but nothing runs | Use `ICommandPipeline` / `CommandScenario`, not a direct decision call |
| Zero operations entered | Authorization, validation, `Provide`, declaration/DI preflight, early commit, scope compatibility; `Validate` never executes work |
| `Recovery` absent | HTTP intentionally omits it; backend processing may not have participated |
| `Incomplete` recovery | Per-operation missing compensator, thrown compensation, or exhausted budget |
| `Suppressed` recovery | Known commit; a later failure is not permission to reverse it |
| `Indeterminate` recovery | Reconcile storage/provider facts; Arc scheduled no durable recovery |
| Cleanup outlives cancellation | Independent cooperative token; check provider cancellation support |
| Work repeats | Caller/reactor retries or provider resilience; verify ownership-safe idempotency |
| Ignored child result still rejects batch | Compose declarations instead of nested commands |
| No cleanup after process loss | Use reactor/outbox/workflow durability rather than an in-memory operation journal |
