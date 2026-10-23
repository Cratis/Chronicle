<!-- cratis-ai-managed: skills/cratis-chronicle-multi-tenancy/references/remove-namespace.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Remove a namespace

Requires Chronicle 19.73.0 for `IEventStore.RemoveNamespace`.
Requires Chronicle 19.75.1 for reminder cleanup.
Requires Chronicle 19.76.1 for concurrent re-subscription recovery.
Requires client 19.77.1 for forgetting a removed namespace on reconnect. Check
with `dotnet list package` and the kernel image tag; older pairs can recreate
an apparently removed namespace.

This is destructive tenant removal, not a way to hide a tenant in the UI.
Before calling it, confirm the named event store and namespace, the authorized
scope, backups and application-owned storage disposition. Never target the
default namespace: its removal is refused. Removing an unknown namespace is a
no-op, and an interrupted removal can be repeated.

Removal first stops listing the namespace, unsubscribes and removes its
observers and their persistent reminders, then drops provider-owned namespace
storage and forgets the namespace. Forwarding and pattern observers with no
saved state are included through their reminders. Re-subscriptions already in
flight are handled by bounded unsubscribe/remove retries.

MongoDB drops its namespace database. SQL and in-memory providers retain their
namespace data; do not claim removal is erasure in those providers. Application
read-model databases are not dropped: the application owns that data and its
separately authorized cleanup.

On reconnect a non-default store asks whether its namespace still exists. If
absent, it releases that store instead of re-registering it; if the kernel
cannot answer, registration follows the older behavior. Explicitly using the
namespace again can create it anew. Stop or reroute writers when removal must
remain final.

Verify removal with a matching kernel and provider, including restart and
reconnect, and separately inspect the disposition of application-owned data.
Do not create or delete a live tenant merely to test this workflow.
