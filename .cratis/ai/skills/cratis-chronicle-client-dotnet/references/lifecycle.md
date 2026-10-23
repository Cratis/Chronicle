<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/references/lifecycle.md -->
# Connection lifecycle

This reference holds registration wiring, the registration wait, keepalive and watchdog behavior, and disposal.

## Lifecycle

- **Registration is wired to the connection, not called by you.**
  `EventStore.RegisterAll` subscribes to `Connection.Lifecycle.OnConnected`
  when `autoDiscoverAndRegister` is on; reconnect re-registers artifacts.
- `DiscoverAll()` does event types first, then constraints, reactors, reducers,
  projections and seeding in parallel; registration is single-flighted with
  jittered backoff/background retry (`EventStore.cs`).
- **Wait with `WaitForRegistration`, not by polling `IsConnected`.**
  `RegistrationWaitExtensions.WaitForRegistration(this IEventStore, TimeSpan? timeout = default)`
  returns `RegistrationOutcome`, including failed runs; its default 5-second
  timeout is for a run that never finished. Inspect `IsSuccess`, `Failure`,
  `Failures` and `IdentityDrift`. It reports projection registrations, not
  reactor/reducer subscription readiness (use `WaitTillSubscribed` / `WaitTillActive`).
  Successful active projection registration includes observer subscription, not
  read-your-writes: inspect the append's `WaitForCompletion` result or read a
  passive model.
  Requires Chronicle 19.65.0 for `IsRegistering`: other members
  retain the preceding run's outcome during re-registration, so readiness needs
  `IsSuccess && !IsRegistering`. Disconnect with automatic registration marks
  it in progress; `WaitForRegistration` waits for that new run rather than
  returning stale success. Check the package version; older clients need
  explicit reconnect-readiness coordination. Source: `Registrations/RegistrationOutcome.cs`.
- **Keepalive is a bidirectional stream plus a watchdog.** The watchdog monitors
  every `MonitorIntervalMilliseconds = 1000`
  (`Source/Clients/Connections/ConnectionWatchdog.cs`) and a keepalive that
  falls more than 5 seconds behind is treated as a lost connection, with
  reconnect backoff capped at 30 seconds. **The failure mode is silence, not an
  exception** — appends keep working while observers go quiet.
- Requires Chronicle 19.72.1 for reactor/reducer `ClientIsNotConnected` stream
  failures to reconnect the whole client with a fresh connection id and register
  all observers again. Older clients can retry a stale registration indefinitely;
  check `dotnet list package` before diagnosing that as a quiet handler.
- `Dispose()` disposes read model reactors for created stores, cancels the owned
  connection, and disposes the connection
  (`Source/Clients/DotNET/ChronicleClient.cs`, `Dispose`).
