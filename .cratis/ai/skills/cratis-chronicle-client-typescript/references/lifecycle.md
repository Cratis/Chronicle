<!-- cratis-ai-managed: skills/cratis-chronicle-client-typescript/references/lifecycle.md -->
# Connection lifecycle

Requires Chronicle.TypeScript 6.51.1 for message-size options; check `yarn why @cratis/chronicle`, or keep gRPC defaults until upgrading.

`ChronicleOptions` and its factory methods accept `maxReceiveMessageSize` and
`maxSendMessageSize`. Both default to 100 MB; pass `null` to keep gRPC's own
default. Check payload bounds before increasing limits; a transport allowance is
not an unbounded-query policy.

This reference holds the watchdog, reconnect, keepalive, compatibility check and telemetry behavior.

## Lifecycle

- A health-check watchdog runs `getVersionInfo` every 5 seconds and reconnects on
  failure; the interval handle is `unref()`'d so it does not hold the process open.
- Reconnect uses exponential backoff with jitter, capped at 30 seconds, rebuilding
  the gRPC channel on every attempt.
- **Keepalive registration is what keeps observers alive.** Losing it triggers a
  full reconnect, because the kernel tears down the client's observers otherwise.
- Failed operations are retried once after reconnecting, keyed on gRPC status
  codes 4/13/14 plus a substring match over the error text.

The client reports its **installed package version**, read from
`@cratis/chronicle/package.json`, and performs `CheckCompatibility` with
`ProtocolVersion` and the contracts descriptor set before a connection is usable
and before event-sequence calls. `IncompatibleChronicleServer` is a permanent
verdict, not retried (`Source/connection/CompatibilityPreflight.ts`).
Three consecutive credential rejections cause `RejectedChronicleCredentials`.
A failed token request is logged and the call proceeds without a token (allowing
an auth-less kernel); tokens come from the selected server, not the first host.

Requires `@cratis/chronicle` 6.43.0 for the built-in spans below.

Built-in spans use `cratis.chronicle.client.*` and scope
`Cratis.Chronicle.Client`; metrics use seconds. Legacy millisecond metrics and
`chronicle.*` attributes are retired except the retained exact-string
`chronicle.sequence_number`. The old constants, `ChronicleMetrics`, and
`telemetry.spanNames` are deprecated; migrate dashboards.
