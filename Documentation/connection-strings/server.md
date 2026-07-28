# Server support

Chronicle connection strings follow this general format:

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
| `least-connections` | Default. Asks every candidate server how many clients it currently has connected and picks the one with the fewest. See below for how the probe works |
| `round-robin` | Cycles through the servers in order, starting at a random offset so a fleet of client instances spreads across the servers. For a small fleet (two clients, two servers) the random offset can still coincide by chance - `least-connections` doesn't have this limitation |
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
| `GET /connections/count` | Returns how many clients are currently connected to *that specific server* - not the cluster-wide total |
| `POST /connections/reserve` | Reserves a connection slot on that server ahead of the client actually connecting |

Before connecting (and on every reconnect), the client asks every candidate server for its count in
parallel and picks whichever answered with the lowest number - ties are broken randomly rather than
always preferring the first candidate. It then calls `POST /connections/reserve` on the server it
picked, before starting the real connect handshake (TLS plus a compatibility check), and only then
proceeds to connect.

The reservation exists because the gap between "decided to connect here" and "actually registered
as connected" is wide enough for another client to probe in the middle of it. Without a
reservation, two clients starting at the same time (a rollout, a composition bringing up several
instances together) can both probe before either has registered, both see the same low count, and
both pick the same server - random tie-breaking alone only turns a guaranteed collision into a coin
flip, it doesn't prevent it. The reservation is reflected in `/connections/count` immediately, so a
second client probing a moment later sees the first client's pick and routes around it. Once the
real connection registers, the server clears the reservation that stood in for it, so a successful
connect is only ever counted once. A reservation that never converts - the client crashes, or
picks a different server after all - is not explicitly released; it simply expires on its own
(30 seconds), so an abandoned connection attempt doesn't permanently inflate a server's reported
count.

Before every probe (not just the first) the client also waits a small random delay, up to 250ms.
This covers the case reservation alone doesn't: two clients whose *probes themselves* land within
microseconds of each other, before either has had a chance to reserve anything - the common case
for a fleet starting together, and just as common when a fleet reconnects together (every instance
was waiting on the same unavailable server and now retries at the same moment it comes back). The
jitter is deliberately applied on every attempt, not only the first, because a synchronized retry
needs the same protection a cold start does.

A server that doesn't answer within 2 seconds, or that can't be reached at all, is treated as
maximally loaded rather than failing the selection - it simply won't be picked over a server that
did respond. This makes `least-connections` a safe default even while a server is restarting or
briefly unreachable: the client routes around it instead of erroring out.

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

- **None**: No credentials provided
- **Client credentials**: Username and password supplied in the authority section
- **API key**: `apiKey` query parameter

You cannot combine client credentials and API key authentication in the same connection string.

## Query parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `apiKey` | string | API key for API key authentication | `?apiKey=your-api-key` |
| `skipTlsValidation` | boolean | Connects over TLS without validating the server certificate | `?skipTlsValidation=true` |
| `loadBalancer` | string | Load balancer strategy when multiple servers are configured | `?loadBalancer=round-robin` |
| `srvNameServer` | string | DNS name server (host[:port], port defaults to 53) for `chronicle+srv` lookups; defaults to the system's name servers | `?srvNameServer=10.0.0.53` |

## TLS

The client always connects over TLS, and by default it validates the server certificate. Set `skipTlsValidation=true` to accept a self-signed or otherwise untrusted certificate — only do this for a trusted server on a trusted network, as it removes protection against man-in-the-middle attacks. The built-in development connection string sets it so development works against the server's self-signed certificate.

See [TLS configuration (client)](../configuration/tls) for certificate setup.
