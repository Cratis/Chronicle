<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/references/connection-options.md -->
# Options and connection strings

This reference holds `ChronicleOptions`, connection string forms and parameters, development credential fallback and certificate validation.

### Options

`ChronicleOptions` — `Source/Clients/DotNET/ChronicleOptions.cs`. Statics:
`FromConnectionString(string)`, `FromConnectionString(ChronicleConnectionString)`,
`FromDevelopmentConnectionString()`. The parameterless constructor uses the
development connection string.

Options worth knowing: `AutoDiscoverAndRegister` (default `true`),
`ConnectTimeout` (5 seconds), `SkipKeepAlive` (short-lived clients),
`DefaultSinkTypeId` (`WellKnownSinkTypes.MongoDB`), `Tls`, `Authentication`,
`RegistrationRetry`, `SkipCompatibilityCheck`, `ArtifactIdentityDrift` (`Warn`),
`UnitOfWorkLifecyclePolicy` (`Compatibility`; `Strict` opts in to completion
checks), `EventGenerationDelivery`, `ExplicitArtifacts`, `ConcurrencyOptions`,
`ClaimsBasedNamespaceResolverClaimType` (`tenant_id`), and
`MaxReceiveMessageSize` / `MaxSendMessageSize` (100 MB each).
`EnableEventTypeGenerationValidation` opts into strict migration-chain checking
on development kernels; production always enforces it.

### Connection strings, not URLs

**There is no `ChronicleUrl` type and no microservice id.** The identity of a
connection is a connection string plus an event store name and optionally a
namespace. `ChronicleConnectionString` lives in `Cratis.Chronicle.Connections`
(`Source/Clients/Connections/ChronicleConnectionString.cs`):

```
chronicle://host[:port]/?opts
chronicle://user:pass@host[:port]/?opts
chronicle+srv://host/?opts
```

Default port `35000`. `Default` is `chronicle://localhost:35000`;
`Development` adds the `chronicle-dev-client` / `chronicle-dev-secret`
credentials. Query options: `apiKey`, `auth`, `skipTlsValidation`,
`skipCompatibilityCheck`, `loadBalancer`, `srvNameServer`, `certificatePath`,
`certificatePassword` (`ChronicleConnectionStringBuilder.cs`). There is an
implicit conversion from `string` and a `Redacted` form for logging.

With ClientCredentials auth and **both** username and password absent, the .NET
client also falls back to these development credentials, including through
`AddCratisChronicleConnection`. Only a `DEVELOPMENT`-compiled kernel creates the
application, directly in persistent storage. Switching that storage to a
production image does not remove it: remove the development application and any
configuration declaration that would recreate it. Do not infer other clients'
authentication behavior from this one. Source:
`Source/Clients/DotNET/ChronicleClient.cs`,
`Source/Clients/Connections/ServiceCollectionExtensions.cs`, and
`Source/Kernel/Core/Setup/Authentication/AuthenticationService.cs`.
Read [host wiring](hosting.md) for the deployment distinction.

> **Certificate validation is skipped by default, and that is deliberate.** The
> development Chronicle images (`latest-development` and `latest-development-slim`)
> generate a self-signed certificate on every start, so a development pair
> connects with no setup. The production image does not: it refuses to start with
> TLS enabled (the default) and no configured certificate. **A production connection string must set
> `skipTlsValidation=false`** (or `Tls.SkipCertificateValidation = false`)
> against a server whose certificate is actually verifiable.
