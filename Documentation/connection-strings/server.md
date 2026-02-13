# Server support

Chronicle connection strings follow this general format:

```text
chronicle://[username:password@]host[:port][/?options]
```

## Components

- **Scheme**: `chronicle://` or `chronicle+srv://` for DNS SRV record lookup
- **Authentication** (optional): `username:password@` for client credentials authentication
- **Host**: The server hostname or IP address
- **Port** (optional): Defaults to 35000
- **Options** (optional): Query string parameters for additional configuration

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

## TLS

TLS is enabled by default. You can disable it for local development using `disableTls=true`, but production deployments should always use TLS.

See [TLS configuration](../hosting/configuration/tls.md) for certificate setup.

