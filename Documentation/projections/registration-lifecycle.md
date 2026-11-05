---
description: Chronicle compares projection definitions when clients connect. Learn when changes rebuild history, how versions protect deployments, and what retirement keeps.
---
# Registration lifecycle

Chronicle compares projection definitions when a client connects, applies changed mappings, and
plans any historical rebuild they require. Registration and catch-up are separate: an accepted
definition does not mean its stored read models are ready to query.

A client registers the definitions its code declares on every connect. Unchanged mappings do not
trigger replay. Before a deployment, check the mapping change, the client's software version, and
whether its full set still declares every projection you intend to keep.

## When a definition changes

A changed definition — including one that *removes* a child collection — is stored and pushed to the
engine on every silo. Chronicle then inspects the change and the namespace's event history before it
decides what to rebuild. Active projection subscribers discard their silo-local cached pipeline
before rebuilding from the changed definition. Newly consumed events therefore use the updated key
resolvers without requiring a kernel restart.

Chronicle checks each namespace's history when deciding what to rebuild:

- No historical action is needed when the only change adds event types and none of those events exist.
- Full replay is required when newly mapped event types have history, or when changes affect event
  ordering, keys, joins, children, removal behavior, or existing mappings.

Even independent new mappings require a full rebuild when they need historical data. Applying only
selected old events to the live container can reject writes behind its watermark or apply
contributions twice. A projection's version or descriptive tags alone do not change its mapping and
do not trigger replay.

The [`definitionEvolution`](../hosting/configuration/observers.md#definition-evolution) policy controls
which plans Chronicle performs automatically. The default, `Automatic`, starts the required full
replay for a non-global projection. Global projections receive a replay recommendation instead.
`PartialOnly` and `Manual` also create a recommendation when a full replay is required. No-action
changes do not create recommendations. Explicit partition replay remains available regardless of
this policy.

Follow full-replay job progress in the Workbench before relying on the rebuilt view. The policy
controls rebuilding history, not whether the new definition is stored or used for newly consumed events.

## When an older client registers — the version guard

During a rolling deploy an old pod can reconnect, or retry a registration, after a newer pod has already
registered. Without a guard the older definitions would overwrite the newer ones, and an older full set
would retire projections the newer version added.

Every registration carries the client's [`SoftwareVersion`](../configuration/chronicle-options.mdx#softwareversion)
and Chronicle stores it with each projection definition. When a registration arrives, Chronicle compares
its version with the stored one by [semantic version](https://semver.org) precedence, ignoring build metadata:

- A projection whose stored version is **greater** than the registration's is left out. The stored definition
  is kept, a warning is logged, the registrant is not recorded, and the Workbench shows a recommendation —
  "Projection registration ignored: projection *X* was registered by an older client version" — once per projection and
  version pair.
- A full set that is older than **any** stored projection of its owner is treated as a partial registration,
  so it retires nothing.
- An **equal**, missing or unparseable version on either side behaves exactly as before: the registration
  replaces the stored definition.
- A newer version replaces the stored definition. A version change alone is only metadata: it is stored, but
  it never triggers a replay.

### Setting the version

The .NET client sends `ChronicleOptions.SoftwareVersion`, which defaults to the entry assembly's informational
version. An application that never sets a version reports `1.0.0`, which gives every release the same number
and so no protection; the client logs a warning at startup when it sees that default. Set a real version in
your build, or override it in configuration:

```json
{
  "Cratis": {
    "Chronicle": {
      "SoftwareVersion": "2.4.0"
    }
  }
}
```

The same override works as the environment variable `Cratis__Chronicle__SoftwareVersion`, or in code through
`options.SoftwareVersion`. The key sits under the `Cratis:Chronicle` section that `AddCratisChronicle` binds by default.

Pre-release and pull request builds are ordered by semantic version rules, so stamp them with a version that
sorts where you expect (for example `2.4.0-rc.1`, which is older than `2.4.0`).

### Rolling back

A rollback that redeploys an older image is an older registration, and Chronicle ignores it. Roll back
by redeploying the previous code **with a higher version**, or set `Cratis:Chronicle:SoftwareVersion` on the
rolled-back deployment to a version above the one you are leaving. Rolling forward with a fix works the same way.

## When a registration partially fails

A failed definition does not roll back other successfully registered projections. Chronicle returns
failures by projection identifier and leaves failed definitions unregistered so the next registration
can retry them.

Inspect the .NET client's [registration outcome](../clients/dotnet/registration-outcome.md) to find the
failed artifact. A failure building a local definition needs a code or configuration correction;
waiting for the kernel to become less busy cannot repair it.

## When the kernel is busy

A newly registered projection can need to catch up with existing events. Registration does not wait
for that work. Catch-up runs on the observer's own turn in a job step that walks the sequence in global
order, rather than processing all that history inside the registration call.

The .NET client retries registration with exponential backoff, configured through
[`RegistrationRetry`](../configuration/chronicle-options). After the foreground attempts are spent,
a failed kernel registration is reported to the caller and can continue retrying in the background
until it succeeds or disconnects. Local preparation failures and identity drift stop that retry path;
they need correction instead. Do not treat an exhausted foreground retry as proof that no artifacts
registered, or repeatedly restart the host to make it catch up.

## When a projection is no longer registered — retirement

A projection the client stops declaring — its read model was deleted, or renamed and thereby given a
new identity — is **retired** when the client next registers its full set:

- Its observer is unsubscribed in every namespace, so it stops consuming events.
- Its jobs are deleted and its failed partitions are cleared.
- Its definition is removed from the engine and from storage.
- Its **sink container is left untouched** — data is never deleted implicitly. Drop or clean the
  container yourself if the data is no longer wanted.

Retirement only happens for a **full-set** registration: the client SDK marks the registration as the
complete set for its owner, and only when every discovered artifact produced a definition. Partial
registrations, such as saving a single projection from the Workbench, never retire anything. A
client-owned full set cannot retire server-owned projections, or vice versa. When the client supplies
a registrant identity, retirement is also limited to projections registered by that application, so
applications sharing an event store do not retire each other's definitions.

### Renamed read models and shared containers

Moving a read model to another namespace changes its full-name identity but can preserve its short
type name and therefore its default **container name**. Renaming the type itself normally changes the
container too: the default .NET naming policy pluralizes the short type name. An explicit fluent
container override can also preserve a shared container.

When the successor targets the same container, retirement stops the old projection and Chronicle
raises a replay recommendation for the remaining projection so that container can be rebuilt from its
definition alone. This does not migrate documents to a newly named container or plan a successor
reducer's rebuild. See [switching read-model styles](choosing-a-read-model-style.mdx#what-it-costs-to-switch-later).
