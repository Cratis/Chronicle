<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/connection-lifecycle.md -->
## Connection lifecycle

Verified at `Chronicle.Kotlin v6.13.0`: `CompatibilityPreflight`,
`ConnectionManager`, `ChronicleConnection` and `EventStore.awaitRegistration`.

- Connect checks kernel contract compatibility with a 10-second deadline;
  incompatibility raises `ChronicleServerIncompatible`.
- Credentials are not validated when constructing the client. A rejected
  connection is surfaced by `awaitRegistration()` and the first append as
  `ChronicleConnectionFailed`. The client keeps retrying and can recover.
- Since `6.11.0`, GUIDs use .NET byte order on the wire. New correlation ids
  round-trip; older events return the first three UUID groups byte-reversed
  relative to what the application originally supplied. Earlier capture ids
  remain stored as-is; supply your original id and the client resolves both.

- **Keepalive is two-way.** The kernel pushes a keep-alive down the `Connect`
  stream and the client answers with a separate unary `connectionKeepAlive` RPC
  (`connection/ConnectionManager.kt:30`). A watchdog checks every
  `WATCHDOG_INTERVAL_MS = 1_000L` and treats a gap longer than
  `KEEP_ALIVE_TIMEOUT_MS = 5_000L` as a lost connection (`:126-128`, `:164`,
  `:171`). **Silence, not an error, is how the connection dies** — nothing throws.
- Reconnect is an infinite loop with jittered exponential backoff, base 1s,
  capped at 30s, re-resolving DNS/SRV on every attempt
  (`ConnectionManager.kt:75-101`, `:155-158`).
- The client identifies itself to the kernel as `"Kotlin"`
  (`ConnectionManager.kt:161`).
- `dispose()` cancels the connection manager, shuts the channel down with a
  5-second `awaitTermination`, then `shutdownNow`
  (`connection/ChronicleConnection.kt:116-125`).
- **Your process must stay alive** for reactors and reducers to keep receiving —
  observation is a live gRPC stream, and each observer runs on its own
  `CoroutineScope(Dispatchers.IO)` (`observation/ReactorsService.kt:57`,
  `observation/ReducersService.kt:59`).
