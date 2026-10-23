<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/references/occurrences.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Durable application occurrences

Requires Chronicle 19.77.0 for the .NET occurrence client and testing surface.
Check with `dotnet list package`; older clients need an upgrade, not an event
appended just to wake a reactor. The kernel must advertise `occurrences` and,
for receipted effects, `effect-receipts`; absent or unknown support fails closed.

## Record and handle

Occurrences are a durable work queue, not domain events or a compliance store.
Trigger payloads are plaintext: never put personal data or operational secrets
in them. Compliance-marked trigger schemas are refused. Keep a stable reference
and resolve protected information through its owning service instead.

```csharp
using Cratis.Chronicle.Occurrences;

[Trigger("refresh-catalog")]
public record RefreshCatalog(string Catalog);
```

Implement `IOccurrenceHandler<RefreshCatalog>` with the signature
`Task Handle(RefreshCatalog trigger, OccurrenceContext context,
CancellationToken cancellationToken)`. Handlers are discovered like reactors. Record
with `eventStore.Occurrences.Record(trigger, idempotencyId)` using a stable
idempotency id. Inspect `Recorded`, `AlreadyRecorded`, `OccurrenceIdConflict`
and `OccurrencePayloadInvalid`; a reused id with different content is not a
successful retry.

The client claims due work and renews its fenced lease while the handler runs.
Failure backs off and eventually quarantines that occurrence without blocking
later work. A stale lease epoch cannot renew, complete or fail another worker's
lease. Delivery can repeat after a crash: a lease is not exactly-once execution.

## Receipt boundary

Use `context.EffectKey(effectName)` for one stable effect name within an
occurrence. `IUnitOfWork.UseEffectReceipt(key, response)` commits the receipt
atomically with transactional events. Zero-event units can also be receipted;
protected reads still have to validate. A repeated effect returns its stored
outcome instead of appending again. Unknown commit results are settled by receipt
lookup, or fail retryably rather than inventing success.

Do not put secrets in a receipt response: it is stored data. An external system
must accept the effect key as its own idempotency key if its effect must survive
redelivery. A Chronicle receipt cannot make arbitrary network I/O atomic.

Read references/immediate-append-refusal.md when a custom receipt boundary must
prevent immediate appends before transport. In Arc, use the
**cratis-arc-command-execution** occurrence executor instead of hand-building the
command's authorization and receipt boundary.

## Startup and shutdown

`Startup` and `Shutdown` are built-in triggers. Set `ChronicleOptions.ActivationId`
to the stable logical deployment activation, not a random id on every restart.
`ActivationScope.PerProcess` instead selects one activation per process.
`LifecycleScope.Application` is the default; `AllNamespaces` durably fans out
over a fixed namespace snapshot, so a crash resumes that fan-out without duplicates.

A logical shutdown is recorded by `IOccurrences.EndActivation(id)`; stopping one
pod is not a logical activation ending. Per-process host shutdown is bounded,
not guaranteed to run during a kill or crash. ASP.NET Core and generic hosts wire
the lifecycle service automatically; standalone clients own their lifecycle.

## Specify retries explicitly

`OccurrenceScenario<THandler>` in `Cratis.Chronicle.Testing.Occurrences` drives
a handler through in-process ingress, claims, receipts and completion:

- `Record(trigger, idempotencyId)` records without delivering.
- `DeliverNext()` claims and invokes; advancing `Time` alone delivers nothing.
- `DeliverWithoutCompleting()` simulates loss after work but before acknowledgement.
- `Replies` can drop a committed append's reply; `Capabilities` on the testing
  store can model current, old or unknown kernel support.
- Register real constructor dependencies in `Services` before the first operation.
  Assert durable events and receipts as well as the delivery outcome.

There are no background workers, automatic lease renewals or reminders in this
scenario; it does not prove behavior in a real multi-node cluster. Run
`dotnet test` on the owning spec project and assert duplicate ingress, redelivery,
terminal refusal, lost reply and unsupported-capability paths.

Requires Chronicle 19.78.0 for `Defaults` and `EventStoreForTesting` constructors
accepting an `EventStoreName` and `EventStoreNamespaceName`. Scenarios built from
such defaults inherit that identity; old constructors retain `testing`/`default`.
