# Connection string elements

[Connection Strings → Server support](../connection-strings/server.md) is the grammar reference —
the scheme, every query parameter, and what each one means. Don't re-derive that grammar from
scratch or from this page; treat it as the spec. This page is narrower: the object model a client
SDK typically wraps around that grammar, using the .NET client's `ChronicleConnectionString` and
`ChronicleConnectionStringBuilder` as the reference shape, since that's the most complete
implementation that exists today.

## Parse into a value, don't pass strings around

Every client so far parses the connection string once, into a typed value, rather than
re-parsing or grep-ing the raw string wherever a setting is needed. The .NET client's parsed shape
exposes:

| Property | Comes from |
|---|---|
| `ServerAddress` / `ServerAddresses` | the host list (single value / full list) |
| `IsSrv` | whether the `+srv` scheme was used |
| `LoadBalancer` | the `loadBalancer` parameter, defaulting to `least-connections` |
| `SrvNameServer` | the `srvNameServer` parameter, for `+srv` lookups |
| `Username` / `Password` | the credentials in the authority section |
| `AuthenticationMode` | **computed**, not stored — derived from which credentials are present |
| `ApiKey` | the `apiKey` parameter |
| `SkipTlsValidation` | the `skipTlsValidation` parameter |
| `CertificatePath` / `CertificatePassword` | client certificate options |

`AuthenticationMode` being computed rather than stored is worth copying deliberately: it means
there's exactly one place that can get the "both credentials and API key present" or "neither
present" cases wrong, and it can never drift out of sync with the fields it's derived from.

## Offer a builder, not string concatenation

Constructing a connection string by hand — string-formatting a host, a port, and a pile of query
parameters — is exactly the kind of thing that quietly produces an invalid or ambiguous string
(wrong separator, double `?`, an unescaped credential). A fluent builder that knows the grammar is
worth offering alongside the parser. The shape worth matching:

```csharp
new ChronicleConnectionStringBuilder()
    .WithHost("chronicle.production.example.com")
    .WithPort(35000)
    .WithCredentials(username, password)
    .WithLoadBalancer("round-robin")
    .Build();
```

with equivalents for `WithServerAddresses(...)` (multiple hosts), `WithDevelopmentCredentials()`,
`WithApiKey(...)`, `WithoutAuthentication()` (`auth=none`), `WithTlsValidationSkipped()`, and
`WithCertificate(path, password)`. `Build()` should validate as it assembles — the same
"credentials and API key together is an error" rule the parser enforces should be enforced here
too, at construction time rather than at first-connect time.

## Never log the unredacted form

A connection string carries the client secret, and possibly an API key or a certificate password —
in full. `ToString()` (or whatever renders it back to a string) should render everything, because
something in the client genuinely needs the real value to connect with. But that real value should
never end up in a log line or an error message as-is.

Offer a second rendering — the .NET client calls it `Redacted` — that keeps everything a log
entry needs to be useful (scheme, host, port, non-sensitive options) and replaces every credential
with a fixed placeholder. Use that form, specifically, everywhere the client itself logs the
connection string — connecting, reconnecting, reporting a connection failure. This is not a
theoretical concern: the .NET client's own log messages used to write the unredacted string on
every connect, and the fix was exactly to route those call sites through `Redacted` instead. A new
client should get this right from its first log statement rather than patch it in later.

## Building blocks worth keeping separate

Two things are easy to conflate but should stay as distinct concepts in the object model:

- **The load-balancer strategy name** (`loadBalancer=round-robin`) selects *which* built-in
  strategy runs. It should also be overridable programmatically with a custom strategy
  implementation for callers who need one the built-ins don't cover — don't make the string
  parameter the only way in.
- **TLS validation** (`skipTlsValidation`) is a connection-string concern, but certificate
  configuration for a client presenting its *own* certificate (`certificatePath`,
  `certificatePassword`) is a related but separate concern from validating the *server's*
  certificate. Keep them as separate settings even though they both live under "TLS" in the query
  string.

See the full parameter table in
[Connection Strings → Server support](../connection-strings/server.md#query-parameters) for every
name, type, and example.

Next: [Documentation and snippets](./documentation-and-snippets.md) covers the last thing a new
client needs before it's ready for other people to use.
