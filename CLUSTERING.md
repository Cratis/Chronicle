# Chronicle Clustering — Status & Remaining Work

This document tracks the state of running Chronicle as a multi-silo Orleans cluster. It exists because
clustering surfaces problems that never appear in the single-silo configuration the kernel has shipped with
so far (`UseLocalhostClustering()` with one silo).

## Why single-silo hides everything

In a single silo, **grain calls never serialize** — Orleans passes arguments by deep copy, and observers,
event sequences and the connected client all live in the same process. The moment a second silo joins:

- grain-call arguments and return values are serialized across the silo boundary, and
- the observer pipeline (event sequence → appended-events queue → observer → connected-client subscriber)
  spans silos and depends on cross-silo coordination staying healthy.

Both of these were effectively untested before. The work on the `copilot/add-integration-specs-clustering`
branch added a real two-silo integration fixture (`Integration/Clustering`) that co-hosts a Chronicle client
on the primary silo, and used it to find and fix the problems below.

## Done — cross-silo serialization

These were latent bugs in the kernel's custom Orleans serializers whose read/write paths had never run.
All are fixed and covered by **deterministic, container-free round-trip specs** under
`Source/Kernel/Core.Specs/Setup/Serialization/` (these are the authoritative proof — they do not need a
cluster and pass every time):

| Type | Bug | Fix |
|---|---|---|
| `ConceptSerializer` | Read path derived the concept type from `typeof(TInput)` (the buffer type), so no concept could round-trip. | Self-describing wire format (concept `Type` + underlying value), with null/reference handling. Also returns `true` from `IsSupportedType`/`IsTypeAllowed` for its own codec type (required so Orleans can encode the type marker). |
| `OneOfSerializer` | `GetCodec(value.GetType())` returned itself → infinite recursion. | Writes concrete type + active index + value; reconstructs `OneOf<…>` and `OneOfBase` derivatives (e.g. `Cratis.Monads.Result<,>`) via reflection. |
| `Cratis.Json.Globals` JSON options | Lazy initializer publishes the options before adding the derived-type converter, so a concurrent serialize freezes it mid-config → "JsonSerializerOptions is read-only". | Pre-warm it single-threaded in `SerializationConfigurationExtensions.Configure`. |
| `OneOf.Types.None` | No Orleans codec; failed-partition recovery jobs return `None` as an ack and could not serialize across silos. | Added `OneOf.Types` to the JSON-serializer predicate. |

## Done — connection / keep-alive resilience (partial)

The clustering integration test is still **not consistently green**, but its dominant failure mode was
diagnosed and substantially reduced. The chain:

1. The kernel keep-alive (`Services/Clients/ConnectionService.Connect`) emits a ping every second and
   **disconnects the client if delivering one throws**. The in-process test connection delivered the ping
   synchronously (`.GetAwaiter().GetResult()`), so a single transient cross-silo `OnClientPing` rejection
   crashed the loop → client disconnected → observer subscribers report `Disconnected` →
   `HandleEventsForPartition` fails with **"Subscriber is disconnected"**.
   - Fixed in `Source/Clients/XUnit.Integration/ChronicleConnection.cs`: keep-alive is now fire-and-forget,
     swallows transient ping failures, re-registers the client when a ping reports it unknown (guarded by
     `lifecycle.IsConnected` so explicit-disconnect reconnect specs are not resurrected), and flags the
     client keep-alive-exempt so the kernel's 5 s timeout never disconnects an in-process client (which
     cannot actually network-drop). This eliminated `FailureDuringKeepAlive`.
2. `ConnectedClients` (grain key 0) holds the connected-client registry **in memory** with no `[KeepAlive]`,
   so it could idle-deactivate (or migrate) and lose every client.
   - Fixed in `Source/Kernel/Core/Clients/ConnectedClients.cs`: added `[KeepAlive]`.

## Remaining work

The integration test still fails intermittently. Known remaining issues, roughly in priority order:

1. **Connection registry is not migration-resilient.** `ConnectedClients` keeps its registry in process
   memory. `[KeepAlive]` stops idle collection but not silo migration / reactivation, which still drops the
   registry. It should be made durable (or rebuilt from heartbeats) so a moving registry grain never
   reports live clients as disconnected.
2. **Mediator re-registration on reconnect.** `IReducerMediator` / `IReactorMediator` route to the
   client-side handler by `ConnectionId`. When connection state is lost and re-established, those handlers
   are not guaranteed to be re-wired, so delivery can return `Disconnected` even after the client is back.
3. **Live event delivery has no reliable recovery.** `AppendedEventsQueue` dispatches to observers with
   direct grain calls and, on failure, "logs and moves on" relying on observer catch-up — but
   `Observer.CheckNextSequenceNumber` only *detects* a gap (updates the tail), it does not reprocess a
   missed live event. A dropped cross-silo dispatch can therefore be lost silently.
4. **Fixture should likely use Orleans `TestCluster`.** The current fixture hand-builds two `Host`-based
   silos with localhost clustering and a co-hosted client, plus retry/warmup scaffolding to paper over
   races. `Microsoft.Orleans.TestingHost.TestCluster` is purpose-built for reliable multi-silo test
   clusters; moving to it (resolving the earlier DI-validation issues) would remove most of the fixture's
   fragility.
5. **Role-based placement is incomplete.** `EventSequencePlacementDirector` / `ObserverPlacementDirector`
   throw when a role is disabled on the *calling* silo, but `GetCompatibleSilos` is not filtered by the
   *target* silo's role, so `Clustering.Roles` does not actually constrain placement. Proper role-based
   placement needs per-silo metadata exposed to the placement directors.

## Notes for whoever picks this up

- **Measure on a fresh machine / CI, not a thrashed dev box.** Reliability of the localhost two-silo
  fixture swings wildly with machine load — the same code was observed passing 11/11 early in a session and
  0/8 after hours of continuous container/silo churn. Prune Docker and restart between long runs.
- **Iterate on serializers with the unit round-trip specs**, never the container-based integration test —
  they isolate serializer behaviour and run in well under a second.
- The integration spec (`Integration/Clustering/for_Clustering/when_appending_an_event_with_reactor_reducer_and_projection.cs`)
  drives a rich payload (concept, enum, collection, nested record) through a reactor + reducer + projection
  so that, when it does pass, it exercises rich-type serialization end-to-end across the silo boundary.
