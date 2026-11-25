<!-- cratis-ai-managed: skills/cratis-chronicle-configuration/references/kernel-options.md -->
# Kernel options

## Contents

- How the kernel reads configuration
- Listener, TLS and features
- Observers, read models, jobs and events
- Authentication
- Clustering and storage
- Development image differences

## How the kernel reads configuration

`chronicle.json` at `/app/chronicle.json` plus `Cratis__Chronicle__*`
environment variables; environment variables win. Keys below are the JSON path
under the root. Defaults describe the production image unless stated.

## Listener, TLS and features

| Key | Default | Change it when |
| --- | --- | --- |
| `port` | 35000 | The port clashes or a proxy expects another. Clients must use the same port. |
| `bindTimeout` | 30 seconds | Startup binds slowly in constrained environments. |
| `healthCheckEndpoint` | `/health` | A probe expects another path. A dedicated health port is configured separately (`health`). |
| `maxReceiveMessageSize` | 100 MiB | Clients send payloads above the limit (`ResourceExhausted`). `null` removes the limit. Keep aligned with the client's send limit. |
| `shutdown.drainTimeout` | 8 seconds | Orchestrator termination grace is shorter or longer than the drain. |
| `tls.enabled` | `true` | Only behind a private HTTPS proxy: `false` serves HTTP/2 cleartext (h2c) and must never be reachable directly. With authentication on, it also needs an external HTTPS `authentication.authority`, or startup is refused. |
| `tls.certificatePath`, `tls.certificatePassword` | not set | The production image refuses to start with TLS on and no certificate. |
| `features.api` | `true` | Turn off the REST API when unused. |
| `features.workbench` | `true` | Turn off the Workbench on a hardened deployment. |
| `features.oauthAuthority` | `true` | Authentication uses an external authority; see below. |
| `features.changesetStorage` | `false` | You need stored changesets and accept the storage cost. |

## Observers, read models, jobs and events

| Key | Default | Change it when |
| --- | --- | --- |
| `observers.subscriberTimeout` | 30 seconds in code | A subscriber is slow to answer. `0` waits indefinitely during catch-up and replay only. The development configuration file sets 5. |
| `observers.maxRetryAttempts` | 10 | Failing observers should quarantine sooner or later. |
| `observers.backoffDelay`, `observers.exponentialBackoffDelayFactor`, `observers.maximumBackoffDelay` | 1 second, factor 2, 600 seconds | Tune retry pacing for transient downstream failures. |
| `observers.definitionEvolution` | `Automatic` | `Manual` or `PartialOnly` when replays must be scheduled by hand. |
| `observers.replayOnDefinitionChange` | `false` in code; `true` in the development configuration file | Decide whether a changed definition replays by itself. |
| `readModels.maxBatchSize` | 500 | Replay batches overload a subscriber or sink; must be above zero. |
| `jobs.maxParallelSteps` | CPU count minus 1 | Cap or raise parallel job steps (replays, rebuilds). |
| `events.queues` | 2 | Event append throughput needs more queues. |

## Authentication

| Key | Default | Change it when |
| --- | --- | --- |
| `authentication.enabled` | `true` | Almost never in production; read the authentication page before turning it off. |
| `authentication.authority` | empty | An external identity provider issues the tokens. Set its issuer URL; non-empty disables the built-in issuer. Never the kernel's own URL. |
| `authentication.initialAdminSetupToken` | not set | First administrator setup through a supplied token. Use secret management; remove after setup. |
| `authentication.adminUser.*` | not set | Bootstrap an administrator by username and password. The password is hashed on first start. |

First administrator on a production kernel: the setup token from the startup
log, a supplied `initialAdminSetupToken`, or the `adminUser` bootstrap. The
development image instead honors a default administrator password (see
[development inner loop](development-inner-loop.md)).

## Clustering and storage

| Key | Default | Change it when |
| --- | --- | --- |
| `clustering.type` | `Localhost` | More than one node: `MongoDB` or `Sql`. Topology belongs to `cratis-chronicle-hosting`. |
| `clustering.siloPort`, `clustering.gatewayPort` | 11111, 30000 | Ports clash or the network policy fixes them. |
| `clustering.gatewayEnabled` | `true` | Applications use only HTTP and gRPC: set `false`. |
| `storage.type` | MongoDB | A different backend. |
| `storage.connectionDetails` | empty | Always in production; prefer `connectionDetailsFile` for secrets. |
| `storage.createIfMissing` | `true` | Databases and schemas must be created by an operator. |

Sinks, per-namespace storage and encryption certificates are covered by
`cratis-chronicle-hosting`.

## Development image differences

- Compile-time `DEVELOPMENT`: self-signed certificate on every start,
  ephemeral keys, built-in default administrator password honored.
- `Webhooks.Validation` and `ExternalServices.Validation` default to `Warn` in a
  development image and `BlockLocalAddresses` in the production image (compile
  time). `AllowInsecureHttp` and the loopback exemption follow the runtime
  environment (`Development`), not the image.
- The development image uses MongoDB at host `localhost`, port `27017` and a
  `chronicle.json` with a 5 second subscriber timeout and replay on definition
  change.
- The development client application created by a development kernel persists
  in storage. Moving that storage to a production kernel does not remove it.
