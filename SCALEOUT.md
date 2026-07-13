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

## Open items / decisions made (flag for review)

- **New dependency**: `DnsClient` NuGet package in `Cratis.Chronicle.Connections` for SRV lookups
  (BCL has no SRV support; hand-rolling DNS is worse). Flagged because dependency manifests are
  otherwise off-limits.
- Round-robin **by partition key** is implemented as deterministic hash-modulo over the ordered
  live connection set (sticky partitions), not a rotating counter — matches "based on the
  partition key" and preserves per-partition ordering.
- `ConnectedClient` gains the silo address (or the per-silo grain key provides it) so the
  Workbench can show clients per server node.
