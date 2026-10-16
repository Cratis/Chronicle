<!-- cratis-ai-managed: skills/cratis-chronicle-hosting/references/operations.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Recovery, retention and upgrades

## Contents

- Observer behavior
- Causation values and incidents
- SQL database ownership
- Upgrade and restore

Source: `Cratis/Chronicle`:
`Documentation/hosting/configuration/{observers,events,clustering}.md`,
`Documentation/hosting/production.md`,
`Source/Kernel/Core/Configuration/Alerts.cs`, and
`Source/Kernel/Core/Alerts/`.

## Observer behavior

`observers.maxRetryAttempts` defaults to 10 per budget (0 is infinite).
`backoffDelay`, `exponentialBackoffDelayFactor` and `maximumBackoffDelay` bound
retries. Clearing a partition quarantine starts a new budget and preserves its
attempt history. Observer quarantine and partition quarantine are separate;
clearing one does not clear the other. Inspect the cause before authorizing clear
or retry. Subscriber timeout abandons waiting, not subscriber work: redelivery
can overlap work that still runs, so side effects must remain idempotent.

Quarantine persists through grain deactivation. Watchdog ticks and catch-up/replay
completion do not end it. An operator clear or a fresh subscription does;
a kernel restart only helps observers that actually receive a fresh subscription.
Event-store subscriptions remain quarantined until operator clear. Recovery can
quarantine again if the cause remains.

Projection replay and catch-up each use a **single globally ordered job step**;
a new catch-up failure stops that step rather than letting other sources proceed.
Full replay is inactive until completion, then routes to catch-up/live unless
quarantined. Partition quarantine stops that partition's automatic retries;
observer quarantine suppresses automatic recovery for the whole observer.
Sources: `Documentation/concepts/observers.mdx`,
`Documentation/projections/registration-lifecycle.md`, and
`Source/Kernel/Core/Observation/Jobs/{CatchUpObserver,ReplayObserver}.cs`.

Requires Chronicle 19.56.0 for authorized `FastForward` on .NET `IReactors`,
`IReducers` and `IProjections` (by observer id or generic artifact), and the
Workbench action. It moves to the sequence tail, discards failed partitions and
keeps built state without invoking skipped events. Inspect `FastForwardResult`
and its outcome: replaying/unavailable is not success. Record the exact observer,
position and skipped scope before authorization; replay from the required history
is a recovery action, not automatic undo. Check the kernel/client version;
older deployments have no fast-forward operation.

Requires Chronicle 19.62.2 for suspected-skip diagnostics:
`chronicle-observer-events-suspected-skipped` and
`chronicle-appended-events-queue-events-possibly-undelivered`, with rate-limited
warnings identifying the affected observer/position/reason. Delivery behavior is
unchanged; absence of a warning does not prove complete historical processing.

Requires Chronicle 19.59.4 for `Shutdown:DrainTimeout`
(`Cratis__Chronicle__Shutdown__DrainTimeout`, default eight seconds), capped below
the host shutdown timeout. It bounds in-flight observer completion and MongoDB
buffer flushing; overdue work is logged and recorded as failed where time allows.
Requires 19.55.2 for catch-up/replay progress to wait for confirmed buffered writes
on every silo and resume from the last durable position after a failed window.
Check the kernel version and shutdown logs; neither guarantee makes an interrupted
external effect exactly once.

### Next-event lookup failures

Routing keeps the saved position and defers observation when
`GetNextSequenceNumberGreaterOrEqualTo` returns a storage error. A watchdog tick
retries that deferred routing only while subscribed and not quarantined. Its
active-observer lookup also leaves the cursor unchanged on error. Only
`GetSequenceNumberError.NotFound` proves there is no relevant event and permits
fast-forward. Read failures are not "nothing to handle"; preserve and investigate
the warning. Sources: `Source/Kernel/Core/Observation/States/Routing.cs` and
`Observer.Watchdog.cs`. This does not implement the proposed Ordered Lanes design
or remove every delivery/completion limitation. Do not claim past omissions
were repaired automatically.

`observers.definitionEvolution` governs projection/reducer changes:

| Policy | Result |
| --- | --- |
| Automatic (default) | applies no-action, partial or full replay classification |
| PartialOnly | applies no-action/partial, recommends full replay |
| Manual | recommends both replay kinds; no-action creates no recommendation |

Changed mappings, key, join, children, filters or reducer fingerprints require
full replay when independence cannot be proved. Deploying a corrected counter
or join does not repair historical state without replay. Reactor/webhook replay
uses `replayOnDefinitionChange` instead; do not confuse the policies.

## Causation values and incidents

`events.causationPropertyRetention` defaults to `Retain`. `Omit` keeps causation
type/time but no property values on **new** appends, forwarded events, revisions
and redactions. It does not rewrite earlier chains. Subject erasure does not
remove those values: they are not encrypted under the subject key. Assess
historical chains separately, with **cratis-chronicle-compliance**.

Kernel `Alerts.IncidentMetricsEnabled` defaults to true. Open incidents and
observer failure/quarantine counters support alerting without dumping event
payloads. Inspect the configured metrics endpoint and incident queries; counters
are operational evidence, not proof that a failed partition recovered. Do not
reintroduce high-cardinality partition labels into exported failure metrics.

## SQL database ownership

Requires Chronicle 19.62.3 for recorded SQL database bindings in the cluster
`DatabaseNames` table and hashed names beyond PostgreSQL's 63-byte/SQL Server's
128-character limits. Existing bindings do not move automatically. An ambiguous
truncated legacy deployment fails with `DatabaseOwnershipUnresolved`; follow the
product's SQL database-name migration guide before restarting, never reset its
storage to bypass the failure. Overlong cluster names fail startup, and incomplete
migrations fail with `DatabaseMigrationIncomplete`. Check the kernel version and
inspect bindings before an authorized migration; older naming can share databases.

## Upgrade and restore

Upgrade every silo together before registering new event generations. Add-only
generation backfill covers only the default namespace's event log; older workers
can still overwrite content during rollout, and upgrading cannot recover content
previously overwritten. See **cratis-chronicle-event-type-migration**.

Restore the certificate ring first, then storage (including Data Protection keys),
then the compliance key store at the storage's point in time. The erasure fence
is inside the key store: an older key backup can resurrect erased data, or miss
new keys. Retain retired certificates as long as a backup needs them. Confirm a
known protected subject remains readable and an erased subject remains erased
before reopening traffic; see **cratis-chronicle-compliance**.
