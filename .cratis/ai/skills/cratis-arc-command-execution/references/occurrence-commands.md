<!-- cratis-ai-managed: skills/cratis-arc-command-execution/references/occurrence-commands.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Execute a command for an occurrence

Requires Arc 22.117.0 and Chronicle client 19.77.2, plus a kernel advertising
`effect-receipts`. Check with `dotnet list package`; older Arc has no occurrence
executor. Do not substitute an ordinary pipeline call and claim durable deduplication.

## Choose the executor

Inject `IOccurrenceCommandExecutor` from `Cratis.Arc.Chronicle.Occurrences` into
an occurrence handler. Its overloads are:

```csharp
Task<OccurrenceCommandResult> Execute(
    object command, OccurrenceContext occurrence, EffectName effectName,
    CancellationToken cancellationToken = default);

Task<OccurrenceCommandResult> Execute(
    object command, OccurrenceContext occurrence, EffectName effectName,
    IEnumerable<string> systemRoles, CancellationToken cancellationToken = default);
```

`OccurrenceContext` and `EffectName` are Chronicle occurrence types. Read
**cratis-chronicle-client-dotnet** for recording triggers and handling lifecycle
occurrences. Do not call the executor from inside another command.

The executor supplies a system principal; no caller roles are inherited. Supply
only the explicit roles the automation needs. The occurrence namespace must
match the resolved command tenant; a mismatch throws. Authorization still runs.

Give each logical effect a stable, unique name within the occurrence. Retrying
uses the same name. Reusing it for a different command returns the earlier
receipt, not the new command's result. A deliberately new attempt at a terminal
business refusal needs a new effect name.

## Outcomes and retry

The executor checks for an existing receipt before running the pipeline. Its
receipt is committed atomically with the command's transactional events.

- `Recorded` and `AlreadyRecorded` are settled results. Their outcomes are
  `Appended`, `NoEvents` or `Refused`.
- Validation, business-rule and append-constraint refusals are terminal and
  recorded. Redelivery does not re-run the handler to reconsider them.
- Authorization failures, concurrency conflicts and exceptions are `Failed`
  without a receipt, so a later delivery can retry. Reread and re-decide rather
  than replaying stale events.
- The stored response is a minimal result, not a raw command response. It does
  not preserve command values or validation messages. Never depend on a receipt
  returning secret data or the original response object.

A receipt settles Chronicle effects, not arbitrary I/O. Two concurrent attempts
can both reach the handler before one wins the atomic commit. Use
`occurrence.EffectKey(effectName).Value` as the external service's idempotency
key where that service supports it; otherwise an external effect can repeat.

## Keep all appends inside the boundary

Return events from the command, or use transactional appends. Do not complete a
stream or commit an aggregate early inside the executor's command.

An immediate append can commit **before** the executor detects it and reports
`OccurrenceCommandAppendedImmediately`. No receipt is recorded for that failed
attempt, so retry can duplicate the escaped append. The executor does not
implicitly provide Chronicle's `RefuseImmediateAppends()` guard. Read
**cratis-chronicle-client-dotnet**'s immediate-append refusal reference when
building a custom boundary that needs prevention before transport.

## Verify

Use an in-process occurrence scenario with the command's real validation and
append behavior. Deliver the same occurrence twice under the same effect name:
assert one committed effect and a recorded result on the second delivery. Cover
a terminal refusal, a retryable concurrency conflict, insufficient capability,
and commit-then-lost-reply recovery. Run the owning specification project with
`dotnet test`; passing means those assertions ran, not just that compilation succeeded.
