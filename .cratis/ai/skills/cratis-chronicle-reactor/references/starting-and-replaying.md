<!-- cratis-ai-managed: skills/cratis-chronicle-reactor/references/starting-and-replaying.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Start at the tail and distinguish replay

Requires Chronicle 19.70.0 for `[StartAtTail]` in
`Cratis.Chronicle.Reactors`. Upgrade the server before the client: an older
server ignores the field and catches up from the beginning. Check with
`dotnet list package` and the kernel image tag.

Put `[StartAtTail]` on a side-effect reactor that should observe only future
events. It changes the **first registration**: an observer already holding a
position keeps it. Leaving the attribute in place is safe, but adding it is not
a command to fast-forward an existing observer. Specify that historical facts
cause no effect on first registration.

`[OnceOnly]` excludes replay, not initial catch-up, failed-partition recovery
or crash redelivery. Keep side effects idempotent. A reconciliation sweep must
not decide from a model that is replaying or behind: rebuilding state can look
like "never processed". Decide from events or a passive model and cover that case.

## Alternate replay behavior

`Cratis.Chronicle.Reactors.ReplayAttribute` targets a method. Mark a second
handler for the same event type when replay must do different work. With that
handler present only it runs during replay; without one, the ordinary handler
runs unless excluded by `[OnceOnly]`. An event type handled only by a `[Replay]`
handler is still subscribed to.

Verify first registration, restart with an existing position, replay and
failed-partition redelivery separately. Assert the external effect count, not
only that the handler was invoked. Run `dotnet test` on the owning spec project;
server-first compatibility and durable positions also need kernel integration.
