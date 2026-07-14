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
| `round-robin` | Default. Cycles through the servers in order, starting at a random offset so a fleet of client instances spreads across the servers |
| `random` | Picks a random server on every connect |

Select the strategy with the `loadBalancer` query parameter, or programmatically through
`ChronicleOptions.LoadBalancerStrategy` for custom implementations of `ILoadBalancerStrategy`.

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
| `disableTls` | boolean | Disables TLS (development only) | `?disableTls=true` |
| `loadBalancer` | string | Load balancer strategy when multiple servers are configured | `?loadBalancer=round-robin` |
| `srvNameServer` | string | DNS name server (host[:port], port defaults to 53) for `chronicle+srv` lookups; defaults to the system's name servers | `?srvNameServer=10.0.0.53` |

## TLS

TLS is enabled by default. You can disable it using `disableTls=true` when TLS is terminated upstream (for example by an ingress or reverse proxy).

See [TLS configuration](../hosting/configuration/tls.md) for certificate setup.
