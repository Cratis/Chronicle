<!-- cratis-ai-managed: skills/cratis-chronicle-client-typescript/references/connection-options.md -->
# Options and connection strings

This reference holds the `ChronicleOptions` factories and bag, the connection string scheme and parameters, and the public subpath exports.

### Options

`ChronicleOptions` has a **private constructor** (`Source/ChronicleOptions.ts`);
use `ChronicleOptions.fromConnectionString(connectionString, options?)` or
`ChronicleOptions.development(options?)`.

The optional bag accepts `telemetry`, `logger`, `clientArtifactsProvider`,
`discoveryPatterns`, `defaultSinkTypeId`, `reactorResultHandler`,
`artifactActivator`, and `readModelNamingPolicy`. `defaultSinkTypeId` defaults
to `WellKnownSinks.MongoDB`.

> `programIdentifier`, `softwareVersion`, and `softwareCommit` exist on the type
> but **neither public factory forwards them**, so they cannot be set.
> Do not write guidance that configures them.

### The connection string

Schemes `chronicle://` and `chronicle+srv://`, default port `35000`
(`Source/connection/ChronicleConnectionString.ts`). Query parameters: `apiKey`,
`disableTls`, `skipTlsValidation`, `certificatePath`, `certificatePassword`,
`loadBalancer`, `srvNameServer`.

Two facts to carry into production guidance:

- **`skipTlsValidation` defaults to `true`.** TLS is on but the certificate chain
  is not validated, because a development kernel serves a self-signed
  certificate. Set `?skipTlsValidation=false` against a real server.
- **A connection string with no credentials silently falls back to the
  development client credentials** (`Source/connection/ChronicleConnection.ts`).
  An anonymous-looking connection string is not anonymous; it is
  `chronicle-dev-client`.

> **There is no `./connection` subpath export.** `ChronicleConnectionString`,
> `ChronicleConnectionStringBuilder`, `AuthenticationMode`, and `LoadBalancerMode`
> are unreachable from the public entry points — `Source/index.ts` never
> re-exports them and `Source/package.json`'s `exports` map has no
> `"./connection"` key. **Application code passes a connection *string*.** Do not
> write an example that imports a connection-string type.

The subpath exports that do exist: `.`, `./events`, `./eventSequences`,
`./eventStoreSubscriptions`, `./compliance`, `./reactors`, `./reducers`,
`./seeding`, `./readModels`, `./projections`, `./jobs`, `./webhooks`,
`./externalServices`, `./identities`, `./observation`, `./sinks`, `./schemas`,
`./types`, `./artifacts`, `./identity`, `./auditing`, `./correlation`,
`./transactions`, `./confidentiality`, `./patterns`, `./testing`, and
`./package.json`. Event-source APIs are root exports; there is no
`./eventSources` subpath. `IncompatibleChronicleServer` and
`RejectedChronicleCredentials` are also root exports.
