<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/references/additional-surfaces.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Additional client surfaces

Source: `Cratis/Chronicle`:
`Source/Clients/DotNET/EventSequences/{IEventSequence,EventForEventSourceId}.cs`,
`Events/{EventSerializer,ICanProvideAdditionalEventInformation}.cs`, `Seeding/`,
and `Documentation/event-seeding/seeding-with-csharp.mdx`. Connection checks:
`Source/Clients/Connections/{ChronicleConnection,CertificateLoader}.cs` and
`Source/Clients/AspNetCore/Diagnostics/ChronicleHealthCheckServiceCollectionExtensions.cs`.

## Tags and ambiguous appends

Requires Chronicle 19.15.0 for named tags.

`AppendWithNamedTags` / `AppendManyWithNamedTags` and
`EventForEventSourceId.NamedTags` carry structured tags. Tags are
plaintext and indexed: never personal data, credentials or business payloads.
Upgrade kernels before clients that depend on named tags. `AppendThroughEventSource`
uses a declared source type and stream name rather than hand-built routing strings.

`Prepare(event)` captures append content; `AppendPrepared(...)` sends that
prepared event. After an ambiguous transport failure, `VerifyContent(sequenceNumber,
preparedEvent, eventSourceId)` reports `Equal`, `Different` or `Unavailable`.
This compares content at a locator, not exactly-once execution. `Unavailable`
is not evidence of absence; never turn it into a blind retry of irreversible work.
`Revise(sequenceNumber, event)` is an authorized mutation, not a retry mechanism.

`GetMetadataAt` reads without payload, one locator or up to **500** locators.
Authorize the read first; a metadata-only response can still expose identities
and causation. Missing events are omitted from the batch. Closing scopes is
covered by **cratis-chronicle-event-constraints**, not by redaction.

## Cross-cutting event properties

`ICanProvideAdditionalEventInformation.ProvideFor(JsonObject @event)` providers
are discovered automatically. The **.NET client** adds their properties in
`EventSerializer` before sending; the kernel does not add them. A different
client must implement its own enrichment contract. Keep domain facts explicit
in the event type and avoid collisions, secrets or personal-data leakage through
unannotated added properties.

## Seed events deliberately

`ICanSeedEvents.Seed(IEventSeedingBuilder)` declares seeds sent on connection and
applied once per namespace. `ForNamespace` targets one namespace; global seeds
also apply to namespaces created later (not a retroactive replay of all history).
Opening a namespace starts their application asynchronously, without an ordering
promise before the first write; scope development/sample data with `ForNamespace`.
Requires Chronicle 19.64.0 client and kernel for authoritative discovered global
sets: each program replaces only its own declaration, including an empty set.
Removing a seeder stops future namespace application, never retracts appended
events. Set a stable, distinct `ChronicleOptions.ProgramIdentifier` per program;
the fallback is the entry assembly name. Manual globals and other programs' sets
remain; identical shared seeds are not multiplied across programs. The first
upgraded authoritative registration clears ownerless legacy globals. Failed
seeder activation/callback keeps the previous declaration, not a partial set.
Check both versions; old clients/kernels retain append-only globals. To clear
stale development declarations, upgrade both, remove or namespace-scope the
seeder, then start the application once.
`For` / `ForEventSource` route by source/stream metadata; `ForEvents` keeps each
wrapper's routing and tags. Routing participates in seed identity, so the same
payload on another stream is another seed. Seeding is a write, not a read-only
connect diagnostic; inspect target namespaces before changing it.

## Connection health and discovery

Calls while disconnected fail fast (`ConnectionTimedOut`, then
`ConnectionUnavailable` after backoff); ASP.NET hosts start while the kernel is
unreachable, log failure and expose the `chronicle` health check.
`AddChronicleHealthCheck(HealthStatus)` honors its configured failure status.
Requires Chronicle 19.59.6 for readiness to require successful artifact
registration as well as connection, and 19.65.0 to stay down during
re-registration (`IsRegistering`). With automatic registration off, an unrun
registration is connected-only; once manually run, its outcome is checked. Check the client package
version; a connected-only probe on older clients does not prove registration.
`IEventStore.Discovery` and startup reports expose discovered artifacts.
`IEventSources` is scoped in host wiring, not a singleton to retain across requests.

Passwordless PKCS#12 client certificates retain their private key on .NET 9+.
A pinned server certificate is trusted only when its chain is the sole validation
failure (`SslPolicyErrors.RemoteCertificateChainErrors` alone): additional
policy errors such as hostname mismatch are not waived. Production clients still set
`skipTlsValidation=false`; pinning is not a general validation bypass.
