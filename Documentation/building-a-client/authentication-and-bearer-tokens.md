# Authentication and bearer tokens

Every call a Chronicle client makes to the Kernel over gRPC needs a bearer token attached to it.
This page describes the exchange a new client has to implement to get one, keep it fresh, and
attach it correctly — reverse-engineered from the .NET client's reference implementation, since
that's where the behavior is defined today.

:::note
This page is about how a **client authenticates to the Kernel**. Chronicle also has
`BearerTokenAuthorization`, `BasicAuthorization`, and `OAuthAuthorization` types in its .NET
client — those configure *outbound* authorization for **Webhooks** and **External Services**
targets the Kernel calls out to. They're unrelated to how a client authenticates itself, and
naming a new client's own types the same way as those would be a confusing coincidence, not a
pattern to follow.
:::

## The three authentication modes

Which mode a connection uses is decided entirely by what's present in the connection string —
there's no separate flag:

| Mode | Connection string carries | What happens |
|---|---|---|
| Client credentials | `username:password@host` (or nothing at all) | Exchanged for a bearer token via OAuth's `client_credentials` grant |
| API key | `?apiKey=...` query parameter | Sent as-is (no exchange) |
| None | `?auth=none` query parameter | No credentials presented at all — only works against a server with authentication turned off |

Supplying both client credentials and an API key in the same connection string is an error
(`AmbiguousAuthenticationMode`); supplying neither and not asking for `auth=none` is also an error
(`MissingAuthentication`) — except that an *empty* connection string is treated as client
credentials rather than an error, because of the development default below. See
[Connection string elements](./connection-string-elements.md) for the full grammar.

## Client credentials really means OAuth client-credentials

It's easy to assume `username:password@host` is HTTP Basic auth. It isn't. The username and
password in the connection string map directly onto an OAuth `client_credentials` grant: username
becomes `client_id`, password becomes `client_secret`, and the client exchanges them for an access
token by calling the Kernel's own token endpoint before making any other RPC:

```text
POST https://{host}:{port}/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id={username}&client_secret={password}
```

The response is parsed for `access_token` and `expires_in` (a missing `expires_in` defaults to
3600 seconds). The client secret is never sent as a Basic-auth header, and it's never sent on
every call — only once, to get a token, which is what then goes on every call.

### Development defaults

A connection string with no credentials at all still resolves to client-credentials mode — it
substitutes two well-known development values rather than connecting anonymously:

```text
client_id     = chronicle-dev-client
client_secret = chronicle-dev-secret
```

`chronicle://localhost:35000` and `chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000`
are exactly equivalent. This is what makes local development work with no setup, and it's also why
those two values are not a secret worth protecting — they're baked into every Chronicle client and
meaningless outside a server explicitly configured to accept them.

## Attaching the token to a call

Once a token exists, it goes on every outgoing gRPC call as a metadata header:

```text
authorization: Bearer {access_token}
```

A gRPC client interceptor is the natural place to do this — it needs to run for unary calls and
every streaming variant (client-streaming, server-streaming, duplex), and it needs one more piece
of behavior beyond just attaching the header: if a call comes back `Unauthenticated`, force a token
refresh and retry the call exactly once before giving up. That single retry is what makes a token
that expires mid-session invisible to the caller instead of surfacing as a hard failure.

## Keeping the token fresh

A well-behaved client refreshes proactively rather than waiting to be rejected:

- Track when the token was issued and how long it's valid for (`expires_in`).
- Treat it as needing renewal a margin *before* actual expiry — a 60-second margin is a reasonable
  default — so a call that starts just before expiry doesn't race the clock.
- If a refresh attempt fails (the token endpoint is briefly unreachable, say), keep serving the
  still-technically-valid cached token rather than failing every call until the endpoint recovers.
  Throttle repeated failed-refresh attempts (a few seconds between retries) so a down token
  endpoint doesn't turn into a tight retry loop on every call.
- Treat an *explicit* refresh request (triggered by the 401-retry above) differently from routine
  proactive renewal: it should bypass the failure throttle, since it's evidence-driven rather than
  a guess that the token might be stale.

### Optional: caching a token to disk

Short-lived processes — a CLI invoked once per command, a script run from cron — pay the full
token-exchange cost on every single invocation if the token only ever lives in memory. A client
worth using from tooling like that benefits from an optional decorator that persists the token to
a local file (client ID, token, and expiry — nothing else) and reuses it across process
invocations until it's genuinely close to expiring, deleting the cache file and fetching fresh on
an explicit refresh. Treat a missing or corrupt cache file as "no token" and fetch normally rather
than failing — a cache is an optimization, never a dependency.

## What a new client needs to implement

- Determine the auth mode from the connection string (client credentials / API key / none), with
  the same "empty means development defaults" and "both present is an error" rules.
- For client credentials, perform the OAuth `client_credentials` exchange against
  `/connect/token` on the configured host.
- Attach `authorization: Bearer {token}` to every call, across every streaming shape your gRPC
  library exposes.
- Cache the token, refresh it proactively ahead of expiry, and retry once on a 401.
- Never log a raw connection string or a raw token — see
  [Connection string elements](./connection-string-elements.md#never-log-the-unredacted-form) for
  why and how the .NET client avoids it.

Next: [Clustering and the connection lifecycle](./clustering-and-connection-lifecycle.md) covers
what happens before and after that first authenticated call — picking a server and staying
connected to it.
