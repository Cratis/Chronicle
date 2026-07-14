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
| `skipTlsValidation` | boolean | Connects over TLS without validating the server certificate | `?skipTlsValidation=true` |

## TLS

The client always connects over TLS, and by default it validates the server certificate. Set `skipTlsValidation=true` to accept a self-signed or otherwise untrusted certificate — only do this for a trusted server on a trusted network, as it removes protection against man-in-the-middle attacks. The built-in development connection string sets it so development works against the server's self-signed certificate.

See [TLS configuration (client)](../configuration/tls) for certificate setup.
