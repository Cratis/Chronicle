<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/references/append-outcomes.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Append outcomes and retry safety

Verified against the tagged sources in `Source/Clients/DotNET/EventSequences/`
(`IEventSequence.cs`, `EventSequence.cs`, `IdempotencyKey.cs`, `AppendResult.cs`,
`AppendManyResult.cs`, `AppendError.cs`) and
[appending events](https://github.com/Cratis/Chronicle/blob/v19.110.1/Documentation/events/appending.mdx).
These are .NET client APIs; do not assume other SDKs expose the same methods.

## Unknown is not failure-before-write

Requires Chronicle 19.82.2 for the distinct unknown-outcome and index-unavailable errors.
Requires Chronicle 19.108.0 for node-loss/timeout unknown-outcome handling.

`AppendError.AppendOutcomeUnknown` means the kernel cannot confirm whether the
events were stored, including an in-flight append interrupted by node loss or
a grain-call timeout. Do not retry an
unkeyed append blindly: read the source's events for the attempt's correlation
id, and append again only when none belongs to that attempt.

An `Errors` entry with value `ConstraintIndexUnavailable` means a governing unique/closing index
still needs repair and **nothing was stored**. Retry unchanged after a short
delay. The kernel repairs constraint indexes from the event log; restarting
clients or removing constraints is not a repair strategy.

## Append once

Requires Chronicle .NET client 19.92.0 and kernel 19.91.0 for keyed appends.

```csharp
using Cratis.Chronicle.EventSequences;

// Keep this key and payload unchanged across retries of this logical operation.
var key = new IdempotencyKey("order-confirmation-42");
var result = await eventLog.AppendOnce(orderId, new OrderConfirmed(), key);
```

- `IdempotencyKey` accepts 1–512 characters. Never put personal data in it.
- Check `IEventSequence.SupportsIdempotentAppends`. A kernel without support
  causes `IdempotentAppendsNotSupported` before sending; do not fall back to
  unkeyed retries when duplicate protection is required.
- `AppendOnce`, `AppendManyOnce`, `AppendOnceWithNamedTags` and
  `AppendManyOnceWithNamedTags` are on `IEventSequence` (and thus `IEventLog`).
  A batch key derives each event key with `IdempotencyKey.ForIndex(index)`;
  preserve the batch's order and content across retries.
- `EventForEventSourceId.IdempotencyKey` keys individual events in a batch,
  including a mixed keyed/unkeyed batch spanning sources. Unkeyed entries do
  not acquire duplicate protection just because other entries have keys.
- `AppendResult.Outcome` / `AppendManyResult.Outcomes` distinguish `Appended`,
  `AlreadyAppended`, and `AlreadyAppendedAfterErasure`. An already-appended
  outcome is success, not permission to re-create an erased event.
- Inspect `IdempotencyConflicts`: a key reused for different content or generation,
  or a match that cannot still be verified, is not a successful retry. A keyed
  retry after an unknown outcome must use the **same** key and content.
- Keys are refused on events staged in a unit of work and on appends carrying
  effect receipts: these throw `IdempotentAppendsNotSupported`, not an unkeyed
  fallback. Keyed appends are not a replacement for DCB or unit-of-work ownership.

Requires Chronicle Testing 19.99.0 for dropped keyed-append replies.

The Testing client supports keyed appends. `DropNext(DroppedReply.Append)` can lose a keyed append reply after persistence;
retry with the same key and assert exactly one stored event and an
already-appended outcome. This proves retry behavior, not an exactly-once
external side effect.

## Several concurrency guards for one source

Requires Chronicle 19.84.0 for multiple concurrency guards for one source.

`AppendMany` overloads accept
`IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>>`. Repeat a source id
for distinct stream guards; a dictionary cannot represent those entries. The
kernel checks the conjunction before any write, preserving batch atomicity.
A violation identifies `ScopeIndex` and `Scope`. Older kernels still refuse
unsupported combinations with `IncompatibleConcurrencyScopesForEventSource`;
never discard a guard to make an append work.
