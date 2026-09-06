# Clustering and the connection lifecycle

[Connection Strings → Server support](../connection-strings/server.md) is the canonical reference
for the connection string format, the load-balancer strategies, and DNS SRV lookup, written for
someone configuring an existing client. This page covers the same ground from the other side: what
a **new client** actually has to implement to participate correctly in a multi-server Chronicle
cluster, and the parts of the picture that page doesn't cover — the wire-compatibility handshake
and the reconnect loop.

## Discovering the server list

A connection string names either an explicit, comma-separated list of hosts (`chronicle://a,b,c`)
or, with the `chronicle+srv://` scheme, a single DNS name to resolve via SRV records — the same
mechanism `mongodb+srv://` uses. For SRV, the client queries `_chronicle._tcp.<host>` and treats
the returned targets, ordered by SRV priority then weight, as the candidate list. **This
resolution happens fresh on every connect and every reconnect**, not once at startup — a server
added to or removed from DNS is picked up automatically the next time the client needs to pick one,
with no client-side configuration change and no restart.

## Picking a server: load balancer strategies

A load balancer strategy is a single operation: given the current candidate list, return the one
server to connect to next. It's invoked on every connect, including every reconnect after a
dropped session — a client doesn't fail over to a fixed backup, it re-runs the same selection.

Implement at least these three; `least-connections` is the sensible default:

- **`least-connections`** — the real default, and the most involved. Every Chronicle server
  exposes two *anonymous* endpoints (anonymous because a client needs to ask before it has
  authenticated): `GET /connections/count` reports how many clients are connected to that specific
  server, and `POST /connections/reserve` reserves a slot ahead of actually connecting. Probe every
  candidate in parallel, pick the lowest count (breaking ties randomly, not by list order), then
  reserve a slot on the winner before starting the real connect handshake. A server that doesn't
  answer within a short timeout (2 seconds is what the .NET client uses) is treated as maximally
  loaded rather than failing selection outright, so a restarting or briefly unreachable server just
  gets routed around. Add a small random jitter (up to 250ms) before every probe, not only the
  first, so that a fleet of clients starting or reconnecting together doesn't send synchronized
  probe bursts. The reservation itself expires on its own after a short window (30 seconds) if it
  never converts into a real connection, so an abandoned attempt doesn't permanently inflate a
  server's reported count.
- **`round-robin`** — cycle through the candidate list in order, but start each client instance at
  a random offset. Without the random start, a fleet of identical client instances all begin at
  index zero and stay in lockstep on the same server.
- **`random`** — pick uniformly at random on every connect. The simplest strategy, and a reasonable
  first one to ship while `least-connections` is still being built.

The full mechanics of the probe/reserve dance — including why the reservation exists at all — are
written out in detail in
[Connection Strings → Server support: How least-connections picks a server](../connection-strings/server.md#how-least-connections-picks-a-server).
A new client's `least-connections` implementation should match that behavior, not just the name.

## The wire-compatibility handshake

Before a connection is usable, the client and server need to agree they're speaking the same
contract — not just the same protocol version, but the same set of services, messages, and fields.
Chronicle does this with an actual structural diff, not a version-number check:

Every contracts package — .NET's included — carries a compiled `FileDescriptorSet` (`chronicle.desc`
in the non-.NET packages) generated from the exact same `.proto` files the server was built from.
On connect, before treating the connection as usable, the client sends its descriptor set to the
server, which parses it and compares it structurally against its own: removed services or methods,
changed method signatures or streaming shape, removed messages, and fields or enum values that were
removed, retyped, relabeled, or renamed (fields are matched by their protobuf field *number*, not
name, so a rename is detected even though the wire format itself wouldn't break). **Pure additions
are never reported as incompatible** — a client is only broken by things it can no longer find, not
by things the server has grown since the client shipped.

If the server reports any incompatibility, the client should raise a hard error and refuse to
proceed — better a clear "your client's contract doesn't match this server" failure at connect time
than a stream of confusing per-call errors later. Against an older server that doesn't support the
compatibility check at all, fall back to fetching its descriptor set and comparing on the client
side instead — don't skip the check entirely just because the newer RPC isn't there.

This is precisely why the contracts package matters so much: your client's copy of `chronicle.desc`
*is* the thing being checked. A hand-generated or stale set of bindings doesn't just risk subtly
wrong types — it risks a hard rejection at connect time, which is the system working as intended.

## Staying connected

A gRPC channel doesn't tell you when the server it's pointed at goes away — TCP can stay
technically open on a server that's stopped responding. Chronicle handles this with a server-pushed
keep-alive stream and a client-side watchdog:

- The server pushes a keep-alive on an open stream at a steady interval. The client just needs to
  notice one arriving.
- The watchdog checks, on a short fixed interval (roughly once a second is enough), whether a
  keep-alive has arrived recently (a threshold of a few seconds — five is a reasonable default). If
  not, treat the session as dropped immediately rather than waiting for an RPC to fail.
- On a dropped session, reconnect with **exponential backoff, capped** — doubling the wait each
  attempt starting from around a second, up to a ceiling around 30 seconds — rather than hammering
  a server that's still coming back up. A cancellation should stop the loop outright rather than be
  treated as a failed attempt to back off from.
- The reconnect path is the *same* connect path described above: re-resolve addresses (DNS may have
  changed), re-run load-balancer selection (the server that dropped isn't guaranteed to be the one
  to reconnect to), and re-run the compatibility check.

## What a new client needs to implement

- Parse both connection-string forms (explicit host list, `+srv`) and re-resolve on every connect.
- Ship at least `least-connections` (matching the probe/reserve protocol) and `round-robin`.
- Embed the compiled descriptor set from your contracts package and run the compatibility check
  before treating a connection as usable; fail hard, don't degrade silently, on a real mismatch.
- Run a keep-alive watchdog with capped exponential backoff, reusing the same connect path for
  reconnects that the initial connect uses.

Next: [Connection string elements](./connection-string-elements) covers the object model most
clients wrap around the string itself.
