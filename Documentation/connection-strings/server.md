---
title: Server support
description: The Chronicle connection string format the server supports, with chronicle:// and chronicle+srv:// schemes, multiple hosts, load balancing and query parameters.
---
Cratis Chronicle connection strings specify server addresses, authentication and connection options. This reference describes the .NET client's behavior.

Connection strings follow this general format:

```text
chronicle://[username:password@]host[:port][,host[:port],...][/?options]
chronicle+srv://[username:password@]host[/?options]
```

## Components

- **Scheme**: `chronicle://` for explicit hosts, or `chronicle+srv://` for DNS SRV record lookup
- **Authentication** (optional): `username:password@` for client credentials authentication
- **Host**: The server hostname or IP address. Multiple hosts can be given as a comma-separated list, each with an optional port
- **Port** (optional): Defaults to 35000
- **Options** (optional): Query string parameters for additional configuration

## Multiple servers and load balancing

When the connection string holds more than one host, the client picks which server to connect to
using a load balancer strategy. The strategy is applied on every connect - including reconnects,
so a client fails over to the next server when the one it is connected to becomes unavailable.

```text
chronicle://node1:35000,node2:35000,node3:35000
```

| Strategy | Description |
|----------|-------------|
| `least-connections` | Default. With multiple candidates, asks each server for its connection count, including pending reservations, and picks the lowest count. See below for how the probe works |
| `round-robin` | Cycles through the servers in order, starting at a random offset. Independently started clients can still pick the same server; `least-connections` uses server counts to reduce this risk, but does not guarantee an even spread |
| `random` | Picks a random server on every connect |

Select the strategy with the `loadBalancer` query parameter, or programmatically through
`ChronicleOptions.LoadBalancerStrategy` for custom implementations of `ILoadBalancerStrategy`:

```text
chronicle://node1:35000,node2:35000?loadBalancer=round-robin
```

### How least-connections picks a server

Every Chronicle Server exposes two anonymous endpoints for this - anonymous because a client needs
to ask before it has authenticated:

| Endpoint | Purpose |
|----------|---------|
| `GET /connections/count` | Returns the connected-client count plus unexpired reservations for *that specific server* - not the cluster-wide total |
| `POST /connections/reserve` | Reserves a connection slot on that server ahead of the client actually connecting |

When more than one candidate remains, the client asks each candidate server for its count in
parallel and picks the lowest count - ties are broken randomly rather than always preferring the
first candidate. It then makes a best-effort call to `POST /connections/reserve` on the server it
picked before starting the connection handshake. A failed reservation does not stop the connection
attempt. With a single candidate, the client connects without probing or reserving a slot.

A successful reservation makes a pending connection visible in `/connections/count` before the
client has finished connecting. Later probes can account for that pending connection instead of
all seeing the same low count. When a real connection registers, the server removes its oldest
outstanding reservation. Reservations not consumed by a connection expire; their lifetime defaults
to 30 seconds and is configurable on the server.

Before every multi-candidate probe, including on reconnect, the client waits a random delay below
250ms by default. This reduces synchronized probes from clients starting or reconnecting together.
The delay and reservations reduce collisions, but cannot prevent concurrent clients from picking
the same server.

A server that doesn't answer within 2 seconds, or that can't be reached at all, is treated as
maximally loaded rather than failing the selection - it simply won't be picked over a server that
did respond. This makes `least-connections` a safe default even while a server is restarting or
briefly unreachable: the client prefers a server that answered. If every probe fails, all candidates
tie at the maximum count and one is selected randomly; selection does not establish that it is reachable.

## DNS SRV lookup

With the `chronicle+srv://` scheme, the client resolves the servers from DNS SRV records instead
of listing them explicitly - the same mechanism MongoDB uses for `mongodb+srv://`. The client
looks up `_chronicle._tcp.<host>` and uses the returned targets and ports as the server list,
ordered by SRV priority and weight. The lookup happens on every connect, so servers added to or
removed from DNS are picked up on reconnect without configuration changes.

```text
chronicle+srv://cluster.example.com
```

For the example above, the client queries the SRV records for
`_chronicle._tcp.cluster.example.com`.

## Authentication modes

Chronicle supports multiple authentication modes. The mode is determined by the credentials present in the connection string:

- **Client credentials**: Username and password supplied in the authority section. With TLS enabled,
  no credentials and no external token endpoint or authority configured, the client falls back to the
  development credentials. An external token endpoint or authority requires explicit client credentials.
  With `disableTls=true` and no credentials, the client instead connects without authentication
- **API key**: `apiKey` query parameter
- **None**: `auth=none` query parameter — the client presents no credentials at all

You cannot combine client credentials and API key authentication in the same connection string. `auth=none`
overrides both, so credentials left behind by a shared configuration cannot quietly turn the exchange back on.

`auth=none` only works against a server running with
[authentication turned off](../hosting/configuration/authentication.md#turning-authentication-off), which is
meant for a Chronicle embedded alongside its own client rather than reachable over a network. Against a server
that enforces authentication, calls to protected services fail as unauthenticated.

## Query parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `auth` | string | Set to `none` to connect without presenting credentials | `?auth=none` |
| `apiKey` | string | API key for API key authentication | `?apiKey=your-api-key` |
| `skipTlsValidation` | boolean | Connects over TLS without validating the server certificate | `?skipTlsValidation=true` |
| `disableTls` | boolean | Connects without TLS, over cleartext HTTP/2 (h2c), to a server running with `tls.enabled=false`. See [Connecting without TLS](#connecting-without-tls) | `?disableTls=true` |
| `skipCompatibilityCheck` | boolean | Skips the connect-time wire-compatibility check against the server | `?skipCompatibilityCheck=true` |
| `skipKeepAlive` | boolean | Skips the keep-alive connection, for short-lived processes. A client that skips it cannot host reactors or reducers | `?skipKeepAlive=true` |
| `loadBalancer` | string | Load balancer strategy when multiple servers are configured | `?loadBalancer=round-robin` |
| `srvNameServer` | string | DNS name server (host[:port], port defaults to 53) for `chronicle+srv` lookups; defaults to the system's name servers | `?srvNameServer=10.0.0.53` |

## TLS

The client connects over TLS unless `disableTls=true` is set, but by default it does **not** validate the server certificate — any certificate, including a self-signed one, is accepted. With TLS enabled, the development images (`latest-development` and `latest-development-slim`) generate a self-signed certificate on every start when none is configured, so this default is what lets a development client connect with no certificate setup. The production image (`latest` and the version tags) generates none and, with TLS enabled, stops at startup without a configured certificate — see [Production deployment](../hosting/production.md). Set `skipTlsValidation=false` to require full certificate chain validation against a server whose certificate is verifiable — do this for anything beyond a trusted network, as the default removes protection against man-in-the-middle attacks. In the .NET client this option and `Tls.SkipCertificateValidation` combine so that whichever one asks for validation wins, so either setting alone is enough and an omitted second setting can never silently turn validation back off.

See [TLS configuration (client)](../configuration/tls.mdx) for certificate setup.

### Connecting without TLS

A server running with `tls.enabled=false` serves cleartext HTTP/2 (h2c) only, for example behind a
TLS-terminating proxy that forwards h2c. Set `disableTls=true` when connecting directly to that
cleartext listener, including through `kubectl port-forward`, for example `chronicle://localhost:35001/?disableTls=true&auth=none`.
Port forwarding does not itself disable TLS; a server with TLS enabled still requires a TLS connection.

Chronicle serves its token endpoint (`/connect/token`) over TLS only. With TLS disabled and authentication turned
on, the server requires an external HTTPS authority. So a client connecting with `disableTls=true` cannot exchange
client credentials:

- With no credentials, the client connects without authentication, as with `auth=none`. It does not fall back to
  the development credentials.
- With explicit client credentials, creating the connection fails with `ClientCredentialsRequireTls`, which names the alternatives.
- With `apiKey=<token>`, the client presents a token you obtained from the server's external authority.

## Compatibility check

Before treating a connection as usable, the client checks that its wire contract and the server's are still compatible — see [Clustering and the connection lifecycle: the wire-compatibility handshake](../building-a-client/clustering-and-connection-lifecycle.md#the-wire-compatibility-handshake) for how the check itself works. This is a real safety check, not a formality: it exists to catch a genuine mismatch, such as a client sent to a server that no longer has fields or methods the client depends on.

Set `skipCompatibilityCheck=true` only as a deliberate, temporary escape hatch — for example when a client that has only *gained* capabilities since the server's release is (incorrectly) refused by a server that cannot be redeployed immediately. Skipping the check does not make an incompatibility disappear; it trades a clear, immediate connect-time failure for whatever less obvious failure the actual incompatibility causes later. Remove the setting once the server is upgraded or the client's assumption about compatibility is otherwise confirmed.
