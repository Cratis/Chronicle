<!-- cratis-ai-managed: skills/cratis-chronicle-configuration/references/client-options.md -->
# Client options

## Contents

- Where client options come from
- ChronicleOptions
- ASP.NET Core options
- Connection-string switches
- Limits that must match the kernel

## Where client options come from

Precedence, lowest to highest: built-in default, `Cratis:Chronicle`
configuration section, connection string, code in the options callback. The
event store name is required and has no default.

## ChronicleOptions

| Setting | Default | Change it when |
| --- | --- | --- |
| `EventStore` (client options) | none, required | Always: name the event store the application writes to. |
| `ProgramIdentifier` | `[N/A]` | Several applications share a kernel and you need to tell them apart in logs and the Workbench. |
| `ConnectTimeout` | 5 seconds | The kernel starts slowly or the network is slow; raise it instead of retrying in application code. |
| `AutoDiscoverAndRegister` | `true` | Registering artifacts explicitly. Set `false` only together with an explicit artifact list. |
| `RegistrationMode` | `Inline` | Registration must not block startup; check the options type before choosing another mode. |
| `RegistrationRetry` | 5 attempts, 2 second initial delay, 30 second maximum delay | Startup races the kernel (compose, orchestrators). Raise attempts or delay rather than adding sleeps. |
| `SkipKeepAlive` | `false` | A short-lived client (a CLI command, a job) that should not hold keep-alive pings. |
| `SkipCompatibilityCheck` | `false` | Connecting deliberately to a kernel version the client does not declare compatible. Leave on otherwise. |
| `UnitOfWorkLifecyclePolicy` | `Compatibility` | `Strict` to fail on units of work that are never completed. |
| `EventGenerationDelivery` | `Compatibility` | Opting into the newer event generation delivery behavior. |
| `ArtifactIdentityDrift` | `Warn` | `Error` in CI to fail on renamed or drifted artifact identities. |
| `DefaultSinkTypeId` | MongoDB | Read models should default to another sink type. |
| `ClaimsBasedNamespaceResolverClaimType` | `tenant_id` | The identity provider carries the tenant in another claim. |
| `EnableEventTypeGenerationValidation` | `false` | Only a development kernel honors this; a production kernel always enforces migration-chain validation. |
| `MaxReceiveMessageSize` / `MaxSendMessageSize` | 100 MiB each | See [limits that must match the kernel](#limits-that-must-match-the-kernel). |
| `Tls.SkipCertificateValidation` | `true` | Any deployed client: set `false` against a certificate that can be verified. |
| `Tls.CaCertificatePath` | not set | The kernel certificate is issued by a private authority the client machine does not trust. |
| `Reactors.MaxConcurrentPartitions`, `Reducers.MaxConcurrentPartitions` | processor count, capped at 8 | Handler code is not thread-safe (set 1) or the machine has spare capacity for more partitions. One partition's batches stay ordered. Client-side only; the kernel `observers` section is separate. |

Telemetry options exist under the client's telemetry section; read the product
documentation for span naming and event-source id tracing before changing them.
The hash key used to pseudonymize event source ids must be at least 32
characters.

## ASP.NET Core options

| Setting | Default | Change it when |
| --- | --- | --- |
| `NamespaceHttpHeader` | `x-cratis-tenant-id` | Your gateway sends the tenant in another header. |
| `UseAutomaticMiddleware` | `true` | You order the Chronicle middleware yourself. |
| Namespace resolver | HTTP header resolver | Tenant comes from a claim, subdomain or fixed value. The header and subdomain resolvers read caller-set values; set or strip the header at a trusted gateway, or use the claims resolver. |
| Unit-of-work `ReportFailuresBeforeResponseStarts` | `false` | The response should fail before it starts when a unit of work fails. |

## Connection-string switches

Connection-string syntax and every query option are in
`cratis-chronicle-client-dotnet` (connection options). The switches that change
runtime behavior:

| Switch | Effect | Use when |
| --- | --- | --- |
| `skipTlsValidation=false` | Verify the kernel certificate | Every deployed client |
| `disableTls=true` | Cleartext HTTP/2 to the kernel | The kernel sits behind a private proxy or port-forward with `Tls:Enabled` false; never across an untrusted network |
| `auth=none` | No authentication | Together with `disableTls`, for a kernel with authentication off |
| `apiKey=<token>` | Bearer token from an external authority | The kernel uses an external identity provider |
| `skipKeepAlive=true`, `skipCompatibilityCheck=true` | Same as the options above | Same as the options above |
| `caCertificatePath=<file>` | Trust a private CA | Private certificate authority |

With `disableTls`, explicit client credentials are refused because they would
travel in the clear. The Elixir client has an extra `authPort` option; the .NET
client does not recognize it.

## Limits that must match the kernel

gRPC message size is enforced on both ends. The client defaults and the kernel
`maxReceiveMessageSize` default are both 100 MiB. When a large event, batch or
read-model payload fails with `ResourceExhausted`, raise the limit on the
receiving side first, then on the sending side, and keep client and kernel
values aligned. A kernel `maxReceiveMessageSize` of `null` removes the kernel
limit.
