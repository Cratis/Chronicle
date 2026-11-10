---
id: 0029-release-idle-unsubscribed-observers
title: Keep subscribed observers resident and release observers that have no subscription and no pending work after an idle period
status: proposed
stage: none
class: working
reversibility: reversible
proposed: 2026-11-10
decided: —
decider: —
applies-to:
  - Source/Kernel/Core/Observation/Observer*.cs
  - Source/Kernel/Core/Configuration/Observers.cs
  - Benchmarks/Chronicle.Benchmarks.Kernel/ObserverFootprint.cs
---

<!-- markdownlint-disable-next-line MD025 -->
# Keep subscribed observers resident and release observers that have no subscription and no pending work after an idle period

Tracking issue: [Cratis/Chronicle#5792](https://github.com/Cratis/Chronicle/issues/5792). Related: #5677, #5110, #4610, #4068.

## Context

A production kernel hosting many namespaces holds 13-29 thousand grain activations per silo and 2-2.4 GiB of live
gen2 heap while it handles under one event per second. The hourly regression of live heap against activations is
46-61 KB per activation (#5792).

### Why observers stay resident

`Observer` carries `[KeepAlive]` (since 2025, replacing `DelayDeactivation(TimeSpan.MaxValue)`), so Orleans never
collects an observer activation, whatever it is doing. Its timers (`Watchdog`, write coalescing) do not keep it alive;
`[KeepAlive]` alone does. Nothing else in the kernel configures grain collection.

The attribute is there because **a subscription exists only in memory**:

- `Observer._subscription` (the subscriber type, the connected clients it delivers to, the silo) is activation state.
  `Observer.ClientSubscriptionRestoration` says so: "the subscription and the superseded registrations live for the
  activation only".
- Live delivery is pushed by the `AppendedEventsQueue` grain, which keeps an in-memory list of subscribed observer keys.
  An observer adds itself in `Observing.OnEnter` and removes itself in `Observing.OnLeave`.
- `Observer.OnDeactivateAsync` transitions to `Disconnected` (except on silo shutdown), which leaves `Observing` and
  unsubscribes from the queue.

So the queue's dispatch does call `GetGrain<IObserver>(key)`, which would activate a released observer, but only for
keys still subscribed. A deactivated observer has unsubscribed itself, and when it is activated again it starts in
`Routing` with no subscription and goes to `Disconnected`. **No push reaches an observer that is not activated.**
Something must call `Subscribe` again:

| Observer kind | What subscribes it again | Cost of releasing it while subscribed |
| --- | --- | --- |
| Client reactor or reducer | `ClientSubscriptionKeeper` in the gRPC service, every `connectedClients.reviseIntervalSeconds` (default 2 s): `IReactor.IsSubscriptionIntact` calls `IObserver.KnowsClient` and restores a lost subscription | Live delivery stops for up to 2 s, then the restore runs a catch-up. No memory saved: the keeper calls the observer every 2 s, so it is activated again at once |
| Projection | `ProjectionsManager` on registration or kernel start only | Live delivery stops until the client registers its projections again |
| Kernel reactor, webhook, event store subscription | Their managers on start or registration only | The same |

### What an activation retains

Measured with `Benchmarks/Chronicle.Benchmarks.Kernel --observer-footprint` (in-process silo with in-memory
storage, 2 000 subscribed reactor observers, forced compacting full collections, `GC.GetTotalMemory`):

| Phase | Live heap over baseline | Per observer | Observer activations |
| --- | --- | --- | --- |
| Subscribed | +83 to +88 MiB | 42-45 KiB | 2 000 |
| Unsubscribed, idle release off (`idleDeactivationAfter: 0`), after 145 s | +61.9 MiB | 31.7 KiB | 2 000 |
| Unsubscribed, idle release after 3 s, after 145 s | +3.3 MiB | 1.7 KiB | 0 |

The per-observer figure matches the production regression. Both runs set Orleans' collection age to 10 s so that the
companion `ObserverAlerts` grain, which every observer activates and which has no `[KeepAlive]`, is collected in
both. Under the default collection age it stays resident for that period too, which is the difference between 42 and
32 KiB. The remaining 1.7 KiB is the in-memory storage's own copy of the observer state, which MongoDB holds outside
the process.

A `dotnet-gcdump` difference between 200 and 2 000 observers attributes about 20 KiB per observer to identifiable
objects. The largest Chronicle-owned item is the per-observer metric scope: 17 instruments, each a
`SingleMetricInstrument` with its own copy of the scope's `TagList` (3.9 KiB per observer under legacy names, twice
that under `Both`). Every other Chronicle-owned object is under 1 KiB: the grain (800 B), the state machine and its
states, and about 30 eagerly allocated, mostly empty, dictionaries and sets (80 B each). The rest is Orleans' own
per-activation bookkeeping: `ActivationData`, the work item group, message queues, the directory entry, timers and
reminder records.

## Decision

1. **Subscribed observers stay resident.** `[KeepAlive]` stays on `Observer`. Releasing a subscribed observer is not
   safe: for projections, kernel reactors, webhooks and event store subscriptions it stops live delivery until a
   restart or a new registration, and for client observers it saves nothing because the subscription keeper activates
   them again within `reviseIntervalSeconds`.
2. **An observer with no subscription and nothing pending is released.** On each watchdog tick, an observer that is
   - not removed, has no subscription, and is `Disconnected` or has no stored observer,
   - not quarantined, not replaying, not catching up, and has no catch-up acquisition in flight,
   - free of failed partitions, alert work, a registered alert reminder and webhook probes, and
   - free of pending state, failure, handled-count or retry-reminder writes,

   starts an idle period. A tick that finds it otherwise starts the period over. Once the period has lasted
   `observers.idleDeactivationAfter` seconds, the watchdog calls `DeactivateOnIdle()`. `OnDeactivateAsync` runs as it
   does for any other deactivation, so the next call activates it from storage exactly as after a silo restart.
3. **The default is 900 seconds; `0` turns it off.** The setting is in `Observers`, so a per-observer retry override
   inherits it, and it is documented in `Documentation/hosting/configuration/observers.md`.
4. **No grain interface or payload changes.** The release is internal to the observer grain; nothing a mixed-version
   cluster exchanges is affected.
5. **The retained bytes are measured repeatably** with the `--observer-footprint` mode of
   `Benchmarks/Chronicle.Benchmarks.Kernel`, documented in its README.

## Options considered

- **Remove `[KeepAlive]` and keep subscribed observers alive with `DelayDeactivation` instead.** Rejected for now. Every
  place that sets or clears `_subscription` would have to arm and disarm the delay, and one missed path silently
  stops a projection's live delivery. Releasing only unsubscribed observers needs no such bookkeeping.
- **Persist the subscription and let the appended events queue activate the observer.** This is what would make
  subscribed observers releasable. It needs a durable subscription record, a queue that keeps keys across observer
  deactivation, and an activation path that restores the subscriber without a catch-up. That is a design change of
  its own, out of scope here.
- **Rely on Orleans' collection age.** Not possible while `[KeepAlive]` is on the grain, and Orleans' notion of idle
  (no calls) does not match ours (no subscription and nothing pending). The subscription keeper calls connected
  observers every 2 seconds, so they are never idle in Orleans' sense anyway.
- **Opt-in only (default `0`).** Rejected. The released state is the one a silo restart or a rebalance already
  produces for every observer, so the path is exercised continuously. Shipping it off would leave the memory of
  disconnected observers resident on every kernel that does not find the setting.
- **Make the metric instruments lazy in this change.** Deferred. It is the largest Chronicle-owned saving per
  activation (about 3.9 KiB, 8-9 %), but `MetricSet`, `SingleMetricInstrument` and `ObserverMetrics` are being changed
  by the bounded observer gauges work (#4610). Doing both at once would conflict; it belongs after that lands.

## Default if unanswered

The implementation ships with a 900-second default. If the record is rejected, setting `idleDeactivationAfter` to `0`
restores today's behavior without a code change: every observer, subscribed or not, stays resident until the silo
stops, and kernels hosting many namespaces keep paying about 40 KiB for each observer whose client has gone.

## Timeline and scope

- **In scope:** releasing idle unsubscribed observers; the configuration option and its documentation; the footprint
  measurement.
- **Out of scope:** releasing subscribed observers (needs a durable subscription, see options); reactor and reducer
  client grains (`Reactor`, `Reducer`), which have no `[KeepAlive]` and are collected by Orleans once their connection's
  keeper stops calling them; the per-observer metric scope (after #4610); the alert reminder registered on every
  activation (#5677).
- **Release:** `minor`. A new setting with a default that changes when resident memory is reclaimed, and no contract
  change.

## Verification

- **Done when:** an observer whose subscription is gone and which has nothing pending is deactivated after
  `idleDeactivationAfter` seconds, a subscribed observer never is, and `0` releases nothing.
- **Verify by:**
  - The specs under `Source/Kernel/Core.Specs/Observation/for_Observer/when_watchdog_runs` covering an idle observer, an
    observer idle for less than the period, a subscribed observer, a period interrupted by a subscription, an observer
    still holding its alert reminder, and the setting at `0`.
  - `dotnet Benchmarks/Chronicle.Benchmarks.Kernel/bin/Release/net10.0/Chronicle.Benchmarks.Kernel.dll --observer-footprint --count 2000 --idle-deactivation 3 --collection-age 10`
    reporting no measured observer activations resident and under 5 KiB per observer after the unsubscribe phase.
  - In production: observer activations per silo falling to roughly the number of subscribed observers after
    `idleDeactivationAfter` plus one watchdog interval, and live heap after GC following them.

## Consequences

- Memory held by observers whose clients have gone, or which were only activated to answer a query, a job callback
  or a reminder, is returned within `idleDeactivationAfter` plus one watchdog interval.
- Subscribed observers still cost about 40 KiB each. On the production kernel in #5792 most of the 17 000 observer
  activations on the busier silo are likely subscribed (five clients registering their reactors and reducers in up to
  20 namespaces, plus projections per namespace), so this release does not remove the bulk of that heap. Reducing the
  cost of a subscribed observer is the next step: the metric scope first, then the companion `ObserverAlerts`
  activation and eager collections.
- An observer released while something still calls it now and then is activated again on the next call, at the cost
  of the storage reads every activation does (definition, state, failed partitions, delivery fences). The idle period
  keeps this from churning.
- The subscription keeper calls every connected observer every `reviseIntervalSeconds` (2 s by default). With
  thousands of client observers that is thousands of grain calls per second on an idle kernel. It is outside this
  decision, but it is why releasing connected observers would gain nothing, and it is worth measuring next.
