<!-- cratis-ai-managed: skills/cratis-chronicle-client-elixir/references/connections.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Connections, trust and resilience

Source: `Cratis/Chronicle.Elixir`: `Source/chronicle/lib/chronicle/connections/{connection_string,connection,keep_alive,client_certificate,transport}.ex`, `registration/coordinator.ex`, and `Documentation/connections/{connection-strings,resilience,crash-reports}.md`.

## Connection strings and authentication

`Chronicle.Connections.ConnectionString` accepts `chronicle://` and `chronicle+srv://`; the default port is `35000`. Multiple comma-separated hosts are resolved again on reconnect; SRV accepts only one host (`connection_string.ex`).

| Function | Line in `connection_string.ex` |
| --- | --- |
| `default/0` — localhost, **no credentials** | |
| `development/0` — well-known local development credentials | |
| `parse/1` | |

Use `development/0` only for a local kernel configured to accept the well-known credentials, never for a shared or production kernel. A bare default connection does not substitute credentials; an authentication-required development kernel drops its session (`:stream_ended`) and it never registers.

Production authentication is URL userinfo `client-id:client-secret@host` (OAuth token exchange and refresh) or the `apiKey` query parameter (an `api-key` header). The API key wins when both are present. URL-encode reserved characters; write literal `+` as `%2B` because userinfo uses form decoding. Obtain values from the application's approved secret mechanism.

Query parameters: `apiKey`, `disableTls`, `skipTlsValidation`, `certificatePath`, `certificatePassword`, `authPort`, `loadBalancer`, `srvNameServer`. `loadBalancer` accepts `least-connections`, `round-robin`, `random`.

## Server trust and client certificates

`skip_tls_validation` defaults to **true** in both the struct and parser (`connection_string.ex`). TLS encrypts traffic but does not authenticate the server with that default. Set `skipTlsValidation=false` outside local development. `disableTls` is only for a TLS-terminating proxy; the kernel itself serves TLS.

Requires `cratis_chronicle` 3.7.0 for client certificates.

`certificatePath` accepts PKCS#12 (`.p12`/`.pfx`) or PEM containing a certificate and matching private key. `certificatePassword` unlocks protected files. The identity is presented to both gRPC and the OAuth token endpoint; invalid files, wrong passwords or mismatched keys fail startup. PKCS#12 loading needs `openssl`. Client certificates cannot be combined with `disableTls=true`.

**A client certificate does not trust the server.** With a client certificate, put custom server-verification options on `GRPC.Credential` passed as `:cred` through `:grpc_options`, not in adapter-level `:transport_opts`. For example, this option excerpt assumes `trusted_der` is an approved DER-encoded CA certificate:

```elixir
grpc_options: [
  cred: GRPC.Credential.new(ssl: [verify: :verify_peer, cacerts: [trusted_der]])
]
```

This credential configures the **gRPC channel only**; OAuth server verification still follows `skipTlsValidation` and the system trust store. Install the CA in that trust store when client credentials need it. Conflicting identity or adapter-level verification overrides are rejected at startup.

Requires `cratis_chronicle` 3.8.0 for the client-owned transport.

The client uses grpc 1.x and Mint 1.11+, owns its transport, and needs no `GRPC.Client.Supervisor`. If the application calls `GRPC.Stub.connect/2` independently, specify `adapter: GRPC.Client.Adapters.Mint` there too (or explicitly depend on Gun). Application-level Mint `client_settings` must retain `enable_push: false`; grpc replaces rather than merges those settings and the client rejects unsafe overrides.

## Credential-safe logging is still necessary

Chronicle's own process status redacts credentials, but dependency crash reports can expose gRPC headers, bearer tokens, API keys and Mint HPACK tables. The client itself installs a primary Logger filter (`Chronicle.Connections.ResponseCrashRedaction`) when a connection starts, replacing the Mint response-process termination report, the one shape it can intercept generically, with a one-line notice (requires `cratis_chronicle` 3.13.0). The `ConnectionProcess` and `GRPC.Client.Connection` state can still print credentials, so before starting the client, install the primary Logger filter from the [client crash-report guide](https://github.com/Cratis/Chronicle.Elixir/blob/main/Documentation/connections/crash-reports.md). It matches `Elixir.GRPC.Client`, `Elixir.GRPC.Channel`, and `Elixir.Mint.` report content and replaces reports with a notice.

Keep `handle_sasl_reports: false`. This is a safety net, not a guarantee: plain warning strings carrying exit reasons are not covered, and filters do not survive a Logger restart. Restrict production log access and rotate any credential already exposed. Never log connection strings.

## Keepalive and readiness

The kernel sends keepalive once a second; the client answers via a separate unary RPC (`keep_alive.ex`). Merely consuming the stream is insufficient. Kernel eviction after five seconds without an answer can silence reactors/reducers while appends continue working. Diagnose the connection when observers go quiet rather than assuming there are no events.

`:connected` means the first keepalive acknowledged the connection id, not that artifacts registered. Failed registration logs `Chronicle registration failed`, stays `:connected`, and retries every five seconds. Wait for `:registered` before claiming readiness.

Reconnect uses exponential backoff (1000 ms base, 10000 ms cap); `:retry_attempts` is accepted but ignored. Appends do not wait or queue during an outage: before a channel exists they return `{:error, :not_connected}`. Read model queries wait up to 30 seconds for registration. Transport failures return `{:error, %GRPC.RPCError{}}`; a timeout does not prove a write was not stored.

Seeders run after `:registered`, again on reconnect, and skip an event source with existing events (`seeding/runner.ex`, `seeding.ex`). This avoids startup duplicates; it does not remove duplicates already stored.
