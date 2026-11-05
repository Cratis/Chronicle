<!-- cratis-ai-managed: skills/cratis-arc-query-paging/references/live-query-processing.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Live query processing

## Shared MongoDB watches

Requires Arc 22.136.0. MongoDB `Observe()` uses a shared database-level watch keyed by deployment and
database rather than opening a collection watch for every observer. Each
observation keeps its filter, paging and tenant context.
Requires Arc 22.137.0 for observations of collections created by the caller's
own `MongoClient` to receive changes. The watcher derives the deployment key
from the database client instead of requiring prior registration. Joined observations coalesce refresh notifications into one pending
refresh and re-read every joined source. Snapshot read failures receive a
bounded retry burst; exhaustion waits for a later notification rather than
spinning or buffering unbounded work.

Watch reconnects resume from the token when possible and request a new snapshot
when the token cannot resume. The watch reopens before asking for snapshots so
writes during the read are still observed. Disposing one observer does not stop
the shared watch; disposing its subject or losing the last subscriber stops that
observation's refresh work and prevents a late emission. Shared watches do not
replace each subscription's cancellation and disposal.

## Intercept the emitted model, not its wrapper

Implement `Cratis.Arc.Queries.IInterceptReadModel<T>` with
`Task<T> Intercept(T readModel)`. Return a copy when transforming data that another
subscriber could share. A non-abstract open generic `IInterceptReadModel<T>`
implementation must target exactly its own type parameter; closed
implementations also work.

Interception runs for supported direct/hub streaming emissions and observable
HTTP snapshot/wait results in the requesting subscriber's scope and identity. Interceptors run before emission guards; a thrown interceptor
fails the result rather than exposing the untransformed value. Ordinary model
queries and Arc-wrapped MVC GETs also intercept results; `[AspNetResult]` opts
out of that result processing. Interceptors are transformations, **not access
control**. Filter rows/fields and authorize independently on every exposed path,
and specify snapshot, SSE/WebSocket and hub paths separately.

## Date-sensitive answers without data changes

Requires Arc 22.138.0 for `Cratis.Arc.Calendar`. Inject `IApplicationCalendar` and
pass an instant to `Read`; it does not read the clock. Zone precedence is an
operation-declared zone, then the first non-null `ICanProvideTenantCalendar`
answer, then `Cratis:Arc:Calendar:TimeZone` (default `UTC`). Headers and query
arguments cannot choose it. Configured and tenant-provider ids must be usable
IANA ids: unknown, Windows-only and nonzero fixed-offset ids fail with
`TimeZoneIsNotAnIanaZone`,
never a silent UTC fallback. Containers need time-zone data (`tzdata`).

A live "due today" answer needs candidates observed **without a clock-dependent
store filter**, or tomorrow's members will never enter the source. Use
`IClockBoundaries.Begin(tenant, operationZone, token)` to obtain a
`ClockBoundaryContext`, then `candidates.ReevaluateAtBoundaries(context,
(candidates, reading) => new ClockEvaluation<TAnswer>(answer, nextBoundary))`.
`LocalDateChanges.NextLocalDateChange(reading.Zone, reading.Instant)` handles
midnight, skipped dates and backwards date transitions. `reading.Today.AddDays`
steps calendar days; subtracting elapsed hours from the instant is different
across daylight-saving changes.

Every next boundary must be strictly after the evaluated instant or null;
otherwise the stream fails with `ClockBoundaryIsNotInTheFuture`. The first result
always emits; equal subsequent answers do not (collections compare in order).
Evaluation is serialized in the context's tenant. The zone is fixed at Begin;
application changes require restart and tenant-zone changes affect new
subscriptions. The operator connects on first subscription and disposes timer
and candidates on last unsubscribe, cancellation, error or completion. It does
not accept `OnNext`. Use the registered `TimeProvider` and a `FakeTimeProvider`
with `Advance` to specify boundary changes without sleeps. This is not a
continuously ticking `now` or an advanceable clock in query scenarios.
