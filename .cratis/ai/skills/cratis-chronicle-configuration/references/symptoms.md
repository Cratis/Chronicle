<!-- cratis-ai-managed: skills/cratis-chronicle-configuration/references/symptoms.md -->
# Symptoms and the setting behind them

## Contents

- Connection and security
- Limits and timeouts
- Observers and replay
- Startup and storage

Each row names the usual setting. Confirm the cause before changing it; a row
is a first hypothesis, not a diagnosis.

## Connection and security

| Symptom | Setting to check |
| --- | --- |
| Client cannot connect to a local development kernel | Port 35000 published on loopback; connection string host and port; kernel running the development image |
| TLS or certificate error from a deployed client | `skipTlsValidation=false` with a certificate the client trusts; `caCertificatePath` for a private CA |
| Client works locally but fails behind a port-forward or proxy | Kernel `tls.enabled` and client `disableTls` must agree; with `disableTls`, use `auth=none` or `apiKey`, not client credentials |
| `ClientCredentialsRequireTls` or credentials refused | `disableTls=true` with explicit username and password; use `apiKey` from an external authority or restore TLS |
| 401 from the Workbench or REST API | Sign-in required; development default administrator on the development image only; production setup token or `adminUser` bootstrap |
| Production kernel refuses to start with TLS on | `tls.certificatePath` and `tls.certificatePassword` missing |
| External identity provider tokens rejected | `authentication.authority` must be the provider's issuer URL, never the kernel's own URL |
| Development application still exists after moving to production | Persisted development client application; remove it and any configuration that recreates it |

## Limits and timeouts

| Symptom | Setting to check |
| --- | --- |
| `ResourceExhausted` or message too large | Raise the receiver's limit first, then the sender's: kernel `maxReceiveMessageSize`, client `MaxReceiveMessageSize` and `MaxSendMessageSize` |
| Client times out connecting at startup | `ConnectTimeout` (5 seconds), `RegistrationRetry`; kernel not yet ready |
| Short-lived client keeps a connection open or pings | `SkipKeepAlive` |
| Version incompatibility error on connect | `SkipCompatibilityCheck` only after confirming the versions are intended |

## Observers and replay

| Symptom | Setting to check |
| --- | --- |
| Subscriber call times out on slow handlers | `observers.subscriberTimeout` (30 seconds in code); development file sets 5 |
| Observer quarantined after repeated failures | `observers.maxRetryAttempts`, backoff settings; fix the handler first |
| Replay did not start after a definition change | `observers.replayOnDefinitionChange`, `observers.definitionEvolution` |
| Reactor or reducer processes one partition at a time or races | Client `Reactors` / `Reducers` `MaxConcurrentPartitions` |
| Replay overloads a sink | `readModels.maxBatchSize`, `jobs.maxParallelSteps` |

## Startup and storage

| Symptom | Setting to check |
| --- | --- |
| Appends fail on a local MongoDB | MongoDB must be a replica set; the bundled development image already is |
| Kernel starts with empty storage connection | `storage.connectionDetails` or `connectionDetailsFile` (empty by default in production) |
| Webhook or capture destination rejected locally | `Validation` mode differs per image; `AllowInsecureHttp` works only in the `Development` runtime environment |
| Several applications indistinguishable in logs | Client `ProgramIdentifier` |
| Tenant resolved wrongly behind a gateway | `NamespaceHttpHeader`, `ClaimsBasedNamespaceResolverClaimType`; never trust a caller-set header or `Host` alone |
