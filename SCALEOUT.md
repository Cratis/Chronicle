# Scale-Out Support — Working Plan

Working document for the `feature/scale-out` branch, addressing the goals in [issue.md](./issue.md).
Follows the precedent of `CLUSTERING.md` as a living status/design doc while the work is in flight.

## Goals (from issue.md)

1. **Load balancing** — clients balance connections across multiple Chronicle server nodes with
   pluggable strategies (round-robin default); implement the documented-but-unimplemented
   `chronicle+srv://` DNS SRV discovery.
2. **Scaled-out clients** — track connected clients per Orleans silo (as grains, silo-local),
   remove immediately on disconnect; observer subscribers fan out to multiple instances of the
   same client with configurable strategies (default: round-robin by partition key).
3. **Workbench** — a server-level page showing connected clients.
4. **Integration specs** — multiple kernel silos + multiple client instances: data arrives,
   observers called correctly per client instance.

## Current state (verified by exploration)

- **Connection string**: `Source/Clients/Connections/ChronicleConnectionStringBuilder.cs` parses a
  single host via `new Uri(...)`; `ChronicleConnectionString.ServerAddress` is a single
  `ChronicleServerAddress(Host, Port)`. Multi-host and `chronicle+srv://` are documented in the
  XML remarks and `Documentation/connection-strings/server.md` but not implemented. No endpoint
  selection abstraction exists. `ChronicleConnection.CreateGrpcChannel()` builds one channel for
  one address; the watchdog reconnect (`StartWatchDog`) always re-targets the same host.
  `OAuthTokenProvider` also derives its token endpoint from the single address.
- **Client tracking**: `Source/Kernel/Core/Clients/ConnectedClients.cs` is a single cluster-wide
  grain at integer key `0` with an in-memory list, pinged via `ConnectionService` keep-alive
  (1s ping, 2s revise timer, 5s staleness cutoff). gRPC stream cancellation already triggers
  `OnClientDisconnected`.
- **Observer delivery**: `Observer` grain (keyed by `ObserverKey`, no connection id) stores a
  *single* `ObserverSubscription` with one `SiloAddress` + one `ConnectedClient`. A second client
  instance registering the same reactor/reducer overwrites the subscription — last writer wins.
  Delivery goes through a silo-pinned `ObserverSubscriberKey` (embeds `SiloAddress`;
  `ConnectedObserverPlacementDirector` pins the subscriber grain) to per-silo `[Singleton]`
  mediators (`ReactorMediator`/`ReducerMediator`) keyed by `(observerId, connectionId, store, ns)`
  that hold the live gRPC stream writer.
- **Partition identity**: `Key` (`Source/Kernel/Concepts/Keys/Key.cs`) / `EventSourceId` flows
  through `Observer.Handling.Handle` per partition — the natural strategy input.
- **Workbench**: no connected-clients surface exists. Server-level pattern to copy:
  `Source/Clients/Api/EventStores/EventStoreQueries.cs` (snapshot + observable `ISubject`) backed
  by a `[Service]` contract in `Source/Kernel/Contracts`, proxies generated into
  `Source/Workbench/Api` on Release build, page pattern `Features/Home.tsx` +
  `DataPage`-based pages, server-level routes in `.frontend/App.tsx` under `BlankLayout`.
- **Integration specs**: `Integration/Clustering/ClusteringFixture.cs` runs a 2-silo in-process
  `InProcessTestCluster` (role-split: event sequences on Silo_0, observers on Silo_1) with
  EphemeralMongo; one client co-hosted in Silo_0 DI via `AddInProcessChronicleClient`. Two client
  instances require one DI container per instance and per-instance signal types.

## Phase 1 — Client-side load balancing + SRV (Source/Clients/Connections)

1. **Multi-host parsing**: support `chronicle://h1[:p1],h2[:p2],.../?options` in
   `ChronicleConnectionStringBuilder` (cannot use `new Uri` on comma lists — parse authority
   manually). `ChronicleConnectionString` gains `IReadOnlyList<ChronicleServerAddress> ServerAddresses`;
   `ServerAddress` remains for compatibility (first address).
2. **SRV scheme**: `chronicle+srv://cluster.example.com` resolves `_chronicle._tcp.<host>` SRV
   records to addresses at connect time (re-resolved on every reconnect so DNS changes are picked
   up). Requires a DNS client capable of SRV queries — BCL has none; plan is the `DnsClient`
   package (same as the MongoDB driver uses) behind an `IChronicleServerAddressResolver`
   abstraction so it stays swappable and testable.
3. **Strategy abstraction**: `ILoadBalancerStrategy` (name TBD to match repo idiom) —
   `ChronicleServerAddress Next(IReadOnlyList<ChronicleServerAddress> addresses)`; default
   `RoundRobinLoadBalancerStrategy`; selected via `ChronicleOptions`/connection-string option.
4. **Wire-up**: `ChronicleConnection.ConnectInternal`/watchdog reconnect asks resolver+strategy for
   the next address; `OAuthTokenProvider` uses the *currently selected* address.
5. Specs for parsing (multi-host, srv scheme round-trip), resolver, and strategy rotation.

## Phase 2 — Per-silo connected-client tracking (Source/Kernel)

1. Re-key `IConnectedClients` from integer key `0` to **per-silo** grains keyed by the silo
   address (parsable string), pinned with a placement director following the
   `ConnectedObserverPlacementDirector` pattern (key carries the silo address).
2. `ConnectionService` (runs on the silo terminating the gRPC stream) registers/unregisters
   against *its local* silo grain via `ILocalSiloDetails`; stream cancellation ⇒ immediate removal
   (plus the existing staleness sweep as a safety net).
3. Cluster-wide view for consumers that need it (Workbench, observer watchdog):
   aggregate over active silos (`IManagementGrain.GetHosts`) fanning out to each silo's grain.
4. Callers to update: `Reactor`/`Reducer` client grains (`[PreferLocalPlacement]` — use local silo
   grain), `Observer.Watchdog`, metrics.

## Phase 3 — Observer fan-out (Source/Kernel/Core/Observation)

1. `ObserverSubscription`: single `(SiloAddress, Arguments)` → a **set** of
   `(ConnectedClient, SiloAddress)` entries. `Subscribe` adds; `UnsubscribeIfMatchesClient`
   removes one entry; observer unsubscribes only when the set drains.
2. **Selection strategy**: `ISubscriberSelectionStrategy` (configurable; default round-robin by
   partition key — deterministic hash of `Key`/`EventSourceId` over the ordered connection set, so
   a partition stays sticky to one client instance while partitions spread across instances).
3. `Observer.Handling.Handle`: pick the connection for the partition, build `ObserverSubscriberKey`
   with *that* connection's silo address, pass the chosen `ConnectedClient` as subscriber context
   metadata (mediators already route by connection id).
4. Resilience: a `Disconnected` result from the chosen connection removes it from the set and
   re-routes the partition to a remaining connection instead of failing the partition.
5. Kernel specs for subscription set mutation and strategy selection.

## Phase 4 — Workbench connected-clients page

1. Contract: server-level operations returning connected clients **per silo** (silo address +
   connection id, version, last seen, debugger flag) — snapshot + observable.
2. Kernel service implementation aggregating the per-silo grains (Phase 2.3).
3. API controller `Source/Clients/Api/Clients/` (`/api/clients`), modeled on `EventStoreQueries`.
4. Release build regenerates proxies → `Source/Workbench/Api/Clients/`.
5. Page `Source/Workbench/Features/ConnectedClients/` (DataPage; columns: server/silo, connection
   id, version, last seen, debugger), server-level route beside `Home` in `.frontend/App.tsx`,
   entry point from the Home page (no server-level sidebar exists today).

## Phase 5 — Integration specs (Integration/Clustering)

1. Fixture support for **N client instances**: separate DI container per client instance (each
   calling `AddInProcessChronicleClient` with its own artifacts/signals), not tied to a silo.
2. Scenarios:
   - Two silos, two instances of the same client app (same reactor/reducer types): events across
     many partitions are delivered **exactly once** across instances; partition→instance mapping
     is sticky; both instances receive work (fan-out actually happens).
   - Instance disconnect: its partitions re-route to the surviving instance; the per-silo
     connected-clients grain reflects the removal immediately.
   - Connected-clients query returns both client instances with their silos (Workbench surface).
3. Reuse warm-up/retry patterns from `ClusteringFixture`.

## Phase 6 — Documentation

- `Documentation/connection-strings/server.md`: multi-host + `+srv` formats, load-balancing
  strategies.
- `Documentation/hosting/configuration/clustering.md` / hosting docs: scaled-out clients,
  fan-out strategies, Workbench connected-clients page.

## Status

- **Phase 1 — done, committed.** Multi-host parsing, `chronicle+srv://` DNS SRV resolution
  (`ChronicleServerAddressResolver`, re-resolved per connect), `ILoadBalancerStrategy` with
  round-robin (random start offset) + random, `loadBalancer` connection-string option,
  `ChronicleOptions.LoadBalancerStrategy`, OAuth token endpoint follows the selected server.
- **Phase 2 — done, committed.** `IConnectedClients` re-keyed per silo (string key = parsable
  silo address, pinned by `ConnectedClientsPlacementDirector`; dead-silo fallback gives an empty
  activation that reports clients as disconnected). Callers updated (ConnectionService,
  Reactor/Reducer via local silo, watchdog via subscription silo, XUnit.Integration).
- **Phase 3 — done.** `ObserverSubscription.Targets` (one `ObserverSubscriberTarget` per client
  instance), `Subscribe` merges compatible client subscriptions instead of overwriting,
  `UnsubscribeIfMatchesClient` removes a single target, watchdog prunes dead targets, live
  handling + `HandleEventsForObserver` + `HandleEventsForPartition` select the target per
  partition via `IObserverSubscriberSelector` (`Observers.FanOutStrategy` config; round-robin =
  deterministic FNV-1a of partition key, sticky per partition and consistent across silos;
  random as alternative). A `Disconnected` result from one instance removes only that instance
  and retries the batch on the remaining ones.
- **Phase 5 — done.** The clustering fixture co-hosts an instance of the same logical client on
  BOTH silos (two instances of every reactor/reducer). New `for_ScaledOutClients` specs verify:
  two connected clients reported on two different silos through `GetConnectedClients`; the
  observer holds two fan-out targets; 20 partitions handled exactly once, spread across both
  instances, and sticky per partition across rounds; and after an instance is removed
  (`UnsubscribeIfMatchesClient`, the same call the kernel makes when a client stream ends), all
  partitions re-route to the remaining instance. The specs flushed out two real bugs, both fixed:
  - **Cross-silo polymorphic payloads were rejected** by the Orleans type manifest
    (`IConstraintDefinition[]` "not allowed") — any remote client registering constraints against
    a grain on another silo would hit this. Fixed with `CratisTypesFilter` (`ITypeFilter`
    allowing Cratis types, matching the JSON codec routing).
  - **Fan-out merge never happened** because `ObserverFilters` record equality compares its
    `Tags` collection by reference — two identical registrations from two instances never tested
    equal. Fixed with structural filter comparison in `Observer.CanFanOutInto`.
- **Phase 4 — done.** Contract `ConnectedClient` gains `SiloAddress`; `IConnectionService` gains
  `GetConnectedClients`/`ObserveConnectedClients` (cluster-wide aggregation over the per-silo
  grains via `IManagementGrain`, observable polls every 2s and only emits on membership change —
  LastSeen excluded from the change identity). `IServices`/`Services` expose `Connections`.
  Api: `Cratis.Chronicle.Api.Clients.ConnectedClient` `[ReadModel]` with `GetConnectedClients` +
  observable `AllConnectedClients`. Workbench: server-level `/connected-clients` page
  (DataPage; Server/Connection/Version/Last Seen/Debugger columns), route beside Home, entry
  button on the Home page.

## Open items / decisions made (flag for review)

- **New dependency**: `DnsClient` NuGet package in `Cratis.Chronicle.Connections` for SRV lookups
  (BCL has no SRV support; hand-rolling DNS is worse). Flagged because dependency manifests are
  otherwise off-limits.
- Round-robin **by partition key** is implemented as deterministic hash-modulo over the ordered
  live connection set (sticky partitions), not a rotating counter — matches "based on the
  partition key" and preserves per-partition ordering.
- Client-side round-robin starts at a **random offset** so a fleet of instances that each connect
  once spreads across servers instead of all picking the first host.
- A definition change from a new client instance (different event types/filters) **replaces** the
  whole subscription (last-writer-wins) — matches the pre-existing rolling-deploy semantics.
- **Pre-existing local toolchain issue**: Release builds fail locally with CS9057 (Cratis.Arc
  20.54.2 generators need a newer Roslyn than SDK 10.0.203) — verified present on a clean tree;
  CI builds with a newer SDK. Proxy generation itself runs before compilation and succeeded.
- OAuth tokens across nodes assume the cluster shares signing keys for `/connect/token` — worth
  verifying server-side when scale-out auth is exercised.

- **Phase 6 — done.** `TestApps/Composition`: an Aspire AppHost running 2 Kernel instances (real
  Orleans/MongoDB cluster, distinct silo/gateway ports), 2 instances of an AspNetCore test app
  (SimpleConsole's capabilities ported to a web UI, plus a per-instance reactor invocation log to
  make fan-out visible), a CoreDNS container serving the `_chronicle._tcp` SRV records the web
  apps resolve through `chronicle+srv://`, and a MongoDB replica-set container. Verified fully
  green end to end: SRV discovery, cross-cluster unique-email constraint rejection, and
  partition-sticky client fan-out across two real processes connected to a two-silo cluster.
  - **`srvNameServer` connection-string option** added (`ChronicleServerAddressResolver` now
    resolves against an explicit name server, not just the system default) — needed because the
    composition's own CoreDNS instance isn't the host's system resolver.
  - **The load-balanced Workbench went through a real design correction.** The first attempt
    stood up a separate `WorkbenchHost` app whose *own connection* round-robinned across both
    kernels — but every Chronicle Server already serves its own Workbench (UI + API) on its main
    port by default (`Features.Workbench`/`Features.Api`), so a third host duplicating that was
    the wrong shape entirely. Corrected to a YARP reverse proxy (`Aspire.Hosting.Yarp`) fronting
    `kernel-1`/`kernel-2` directly with `RoundRobin` load balancing — "one frontend, load balanced
    on top" now means exactly that. Getting there surfaced three more real, previously-invisible
    bugs:
    - Declaring the kernel's port as an Aspire endpoint defaults to a **DCP-proxied** endpoint;
      since the kernel binds that exact port itself (custom Kestrel, not the ASPNETCORE_URLS
      convention), the proxy and the kernel's own listener silently fought over the same port and
      every connection black-holed. Fixed with `IsProxied = false` on the kernel's endpoint —
      the same fix already used for the `mongodb`/`dns` containers in this same file.
    - YARP's multi-destination `AddCluster(name, destinations)` overload does **not** call
      `WithReference` the way the single-destination overloads do, so the proxy container never
      received the `services__kernel-N__https__0` env vars it needed — it tried a literal DNS
      lookup for the hostname `kernel-1` and failed. Fixed with explicit `.WithReference(kernel1)`
      / `.WithReference(kernel2)` on the YARP resource.
    - **The Kernel Server's embedded Workbench was non-functional in this codebase** — it called
      `UseDefaultFiles()`/`UseStaticFiles()` with no `FileProvider`, which looks for a *physical*
      `wwwroot` next to the built DLL that is never populated (the Workbench frontend's build
      output — and the only place it gets embedded — is `Cratis.Chronicle.Workbench`, consumed
      previously only by the standalone `WorkbenchHost`). Fixed by having `Server.csproj`
      reference `Clients/Workbench/Workbench.csproj` and serving its embedded
      `Cratis.Chronicle.Workbench.Files` resources via `ManifestEmbeddedFileProvider` — the same
      pattern `WebServer.cs` already used for standalone hosting. This is a genuine product fix,
      not composition-only plumbing: any Kernel Server deployment relying on `Features.Workbench`
      to serve its own UI was previously getting a 404.
  - `TestApps/WorkbenchHost` is kept (not deleted) — it demonstrates the still-legitimate
    standalone-hosting case (Workbench pointed at an already-running/remote cluster without
    embedding a Kernel), and is what originally exercised the `Connections`/`Workbench` library
    fixes below. Composition just no longer uses it as the "load-balanced Workbench," since the
    kernels already provide that surface directly.
  - Cross-process verification (real multi-process Orleans cluster, real TCP, real reconnects)
    surfaced bugs invisible to every previously-passing in-process spec:
    - `JobsManagerExtensions.StartOrResumeObserverJobFor` called `.AsT0` unconditionally on a
      `Result<JobId, StartJobError>`, crashing the second node to join a cluster whenever a job
      start legitimately lost a race to the first node.
    - `ConcurrencyScopesSerializer.ReadValue` read an extra field header the engine had already
      consumed, misaligning the stream only when a `Seed` call crossed a real silo boundary.
    - Standalone Workbench hosting (`WebServer.cs`) had three bugs only visible outside a single
      shared-connection process: `AddCratisChronicleApi()`'s default `useGrpc: true` created its
      own connection to `localhost:35000`, shadowing the host's actual connection; eager DI
      container validation rejected convention-based registrations; and the generated API route
      prefix didn't match the frontend proxies' expected shape.
    - `AddCratisChronicleConnection` never constructed an `OAuthTokenProvider`, so any raw/
      standalone host (Workbench, `WorkbenchHost`) got 401s. Fixed by building one from the
      connection string's client-credentials the same way `ChronicleClient` does.
    - `IServices` (and its 15 sub-services) were registered `AddSingleton`, caching gRPC proxies
      bound to the *first* channel — a reconnect/failover disposed that channel and every
      subsequent call threw `ObjectDisposedException`. Fixed by making them `AddTransient`
      (resolved live from the current connection on every use).

## Phase 7 — Least-connections load balancing (default) — done

Prompted by `TestApps/Composition` reliably showing both web apps' connections landing on the
*same* silo — round-robin's random starting offset collides about half the time with only two
servers, which is exactly the small-fleet case this composition demonstrates.

- **Aspire dashboard**: fixed port (`ASPNETCORE_URLS=http://localhost:18888`) and anonymous
  access (`ASPIRE_ALLOW_UNSECURED_TRANSPORT` + `ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS`), set
  before `DistributedApplication.CreateBuilder` in `TestApps/Composition/Program.cs` — local-dev
  convenience only, so every run uses the same URL with no login-token query string to copy.
- **Connected-clients query is now observable**: `ConnectionService.ObserveConnectedClients`
  polls `GetConnectedClients` (aggregating across all silos via `IManagementGrain.GetHosts`) on a
  1-second cadence (was already `ISubject`-shaped; the interval is what changed, from 2s).
- **`least-connections` strategy** (`Source/Clients/Connections/LeastConnectionsLoadBalancerStrategy.cs`,
  new, now the **default** — `LoadBalancerStrategies.Create(null/"")`): asks every candidate
  server `GET /connections/count` in parallel and picks the lowest, breaking ties randomly (not
  always the first candidate — a fleet starting together would otherwise all tie and all pick the
  same one deterministically). Before the real connect handshake it calls
  `POST /connections/reserve` on the server it picked, and waits a small random delay (up to
  250ms, every attempt — not just the first) before every probe. Both endpoints are anonymous
  (`AllowAnonymous()` on `Kernel/Server/Program.cs`) since a client asks before authenticating.
  `ILoadBalancerStrategy.Next` became `async` for this (round-robin/random adapted trivially).
  `IConnectedClients` (per-silo grain, `Source/Kernel/Core/Clients`) gained `GetConnectionCount()`
  and `ReserveConnection()`; a reservation lives 30s unless cleared earlier.
- **Bug found via empirical composition testing, not specs**: `GetConnectionCount()` summed
  `_clients.Count + _reservations.Count`, but nothing ever cleared a reservation once the real
  connection it stood in for actually registered — so every successful connect inflated its own
  silo's reported count by one for up to 30 seconds afterward. This didn't cause bad *decisions*
  (a client that made a reservation minutes ago has long since expired it), but it made the raw
  `/connections/count` numbers misleading to read during/right after a burst of connects — which
  is exactly when a human (or a diagnostic script) is most likely to be watching them. Fixed by
  clearing the oldest outstanding reservation in `OnClientConnected`.
- **The real lesson of this phase was measurement, not the algorithm.** Five consecutive
  composition runs looked like collisions before this was root-caused; the actual decision logic
  (verified by instrumenting `Next()` directly) was correct every time it was checked this way.
  Two separate verification bugs produced false failure signals:
  - The kernel's HTTP endpoints are multiplexed on the *same* Kestrel port as gRPC
    (`ASPNETCORE_URLS=https://*:35001`, not the Orleans gateway port `30001`) and require HTTPS —
    probing `http://localhost:30001/connections/count` (the gateway port, over plain HTTP) never
    reaches the mapped endpoint at all.
  - `lsof -p <pid> | grep -oE '35001|35002' | head -1` is not a reliable way to identify which
    server a client ended up connected to — the HTTP probe/reserve calls open their own short-lived
    connections to *both* candidates, so `head -1` can report a stale or incidental file descriptor
    instead of the actual long-lived gRPC channel. Cross-checking against the kernel-reported
    `/connections/count` (the authoritative source) resolved the ambiguity.
  - Once probing the right scheme/port and reading the authoritative per-silo count, three
    consecutive cold-start `TestApps/Composition` runs (kernels and both web apps launched
    together by Aspire, the original failure scenario) each split 1-1 across the two silos.
- `LoadBalancerStrategies.Create` defaults to `least-connections` when no `loadBalancer` query
  parameter is given (was `round-robin`); `TestApps/Composition`'s connection string spells the
  option out explicitly anyway, to demonstrate it by example rather than rely on the default.
- Documented in `Documentation/connection-strings/server.md` — strategy comparison table,
  the probe/reserve/jitter mechanics, and why a server that doesn't answer is treated as maximally
  loaded rather than failing selection.
