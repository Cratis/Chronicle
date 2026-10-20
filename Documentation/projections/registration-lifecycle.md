---
description: How Chronicle handles projection registration when clients connect, including changed definitions, the version guard for older clients, and rollback.
---
# Registration lifecycle

Clients register their projections on every connect — the full set of definitions the client's code
declares. Chronicle compares each incoming definition against the registered one and only acts on
what changed, so a re-registration from an unchanged client is near-free.

## When a definition changes

A changed definition — including one that *removes* a child collection — is stored and pushed to the
engine on every silo. Chronicle then inspects the change and the namespace's event history before it
decides what to rebuild. Active projection subscribers discard their silo-local cached pipeline
before rebuilding from the changed definition. Newly consumed events therefore use the updated key
resolvers without requiring a kernel restart.

The rebuild decision is:

- **No action** when the only change adds event types and no events of those types exist yet.
- **Partial replay** when only new event types were added, their explicit property mappings do not overlap
  existing mappings, and every read-model instance is isolated to one event source. Chronicle applies only
  the newly consumed event types to event sources containing those events.
- **Full replay** for changes that can affect event ordering, keys, joins, children, removal behavior,
  or existing mappings. Full replay is the safe fallback whenever Chronicle cannot prove independence.

The [`definitionEvolution`](../hosting/configuration/observers.md#definition-evolution) policy controls
which plans Chronicle performs automatically. The default, `Automatic`, applies all three. `PartialOnly`
performs safe partial work but creates a recommendation for a full replay, while `Manual` creates a
recommendation for any replay. Chronicle reports the chosen classification in its logs, and partial and
full replays use the existing observer job progress shown in the Workbench.

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

Definitions are isolated from each other: a definition the engine rejects fails *alone*. Every other
definition in the registration still lands — in the engine, in its projection grain, and in the
registered state — and the failure is reported back to the client naming exactly the projections that
did not register. A failed definition is not marked registered, so the next registration retries it.

## When the kernel is busy

Registering a projection against a store that already holds events starts a catch-up, and registration
does not wait for it. A projection catches up through a single job step that walks the sequence in
global order, and that step is brought up on the observer's own turn rather than inside the
registration call — so the size of the event log does not decide how long registering takes.

Registration is also retried rather than fatal. The client re-runs the whole registration with an
exponential backoff before giving up, configurable through
[`RegistrationRetry`](../configuration/chronicle-options) — every step is idempotent, so a retry
costs the kernel only the comparison it already does. A host therefore waits out a kernel that is
merely busy instead of failing to start, restarting, and re-registering into the queue it was waiting
on. A kernel that is genuinely refusing the definitions still fails the host, once the attempts are
spent.

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
registrations — such as saving a single projection from the Workbench — never retire anything, and a
client-owned full set never retires server-owned projections (or the other way around).

### Renamed read models and shared containers

Moving a read model to another namespace changes its full-name identity but can preserve its short
type name and therefore its default **container name**. Renaming the type itself normally changes the
container too: the default .NET naming policy pluralizes the short type name. An explicit fluent
container override can also preserve a shared container.

When the successor targets the same container, retirement stops the old projection and Chronicle
raises a replay recommendation for the remaining projection so that container can be rebuilt from its
definition alone. This does not migrate documents to a newly named container or plan a successor
reducer's rebuild. See [switching read-model styles](choosing-a-read-model-style.mdx#what-it-costs-to-switch-later).
