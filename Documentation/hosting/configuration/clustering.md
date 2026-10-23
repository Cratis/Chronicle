---
description: Configure how multiple Chronicle Server nodes form one Orleans cluster and distribute workloads by role for horizontal scale out.
---
# Clustering

Clustering configuration controls how multiple Chronicle Server nodes form one Orleans cluster and how grain workloads are distributed across them. This enables horizontal scale out and assigning specific roles to different nodes—for example, dedicating some nodes to event sequences and others to observers (reactors, reducers, projections).

> [!WARNING]
> The clustering type defaults to `Localhost`, which is single-node membership, regardless of storage
> type. A deployment with multiple nodes must explicitly select `MongoDB` with MongoDB storage, or `Sql`
> with SQL Server or PostgreSQL storage, on every node. Nodes left on `Localhost` do not join one cluster,
> even when they share storage. The localhost-clustering warning is only logged for non-local MongoDB
> storage; it is not a safeguard for SQL deployments.
>
> All nodes must share the same cluster database, `clusterId` and `serviceId`. Configure the type through
> `clustering.type` in `chronicle.json` or `Cratis__Chronicle__Clustering__Type`. Unsupported combinations
> fail at startup: `Sql` requires `MsSql` or `PostgreSql` storage, and `MongoDB` requires MongoDB storage.
> SQLite and in-memory storage do not support multi-node clustering.

## Use cases

- **Horizontal scaling**: Run multiple nodes as one cluster; separate event ingestion (event sequences) from event processing (observers) to scale independently based on load
- **Resource isolation**: Run observers on nodes with different resource profiles (e.g., more memory for complex projections)
- **Testing**: Validate multi-node behavior in integration tests by enforcing deterministic grain placement

## Example configuration

```json
{
  "clustering": {
    "type": "MongoDB",
    "siloPort": 11111,
    "gatewayPort": 30000,
    "clusterId": "chronicle",
    "serviceId": "chronicle",
    "roles": {
      "eventSequences": true,
      "observers": true,
      "jobs": true
    }
  }
}
```

### SQL clustering

For SQL Server or PostgreSQL storage, explicitly set `clustering.type` to `Sql`:

```json
{
  "storage": {
    "type": "PostgreSql",
    "connectionDetails": "Host=database;Database=chronicle;Username=chronicle;Password=secret"
  },
  "clustering": {
    "type": "Sql",
    "clusterId": "chronicle",
    "serviceId": "chronicle",
    "defunctSiloCleanupPeriod": "01:00:00",
    "defunctSiloExpiration": "03:00:00"
  }
}
```

For SQL Server, use `storage.type: MsSql` and its connection details instead. Every node must connect to
one shared cluster database (including the same configured SQL schema), not a per-node database.
Membership uses Chronicle's SQL connection resolution and schema provisioning. Silo and gateway ports
must be reachable between nodes; use `advertisedIP` when the default address is not reachable.

The defunct-silo cleanup settings apply to both `Sql` and `MongoDB`: the sweep runs hourly by default
and removes defunct entries older than three hours. Set the cleanup period to `00:00:00` to disable it.
The development kernel state reset is refused under `Sql` clustering because it would erase live
membership. Use an isolated `Localhost` cluster for development resets. To replace the nodes of a `MongoDB` or `Sql` cluster, see the [rolling-deploy runbook](../rolling-deploy.md), which starts with which versions can run side by side.

After a node is lost, observers can lag behind the event log until the observer watchdog catches them up. This applies to every clustering type and is tracked in [Chronicle#5246](https://github.com/Cratis/Chronicle/issues/5246).

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| type | string | Localhost | Cluster membership. `Localhost` is single node development clustering. `MongoDB` keeps membership in MongoDB storage. `Sql` keeps membership in the SQL Server or PostgreSQL cluster database. Multi-node clustering is explicit opt-in; all nodes must share the cluster database and cluster identity. |
| jobsFallback | string | AnySilo | Fallback when no Jobs silo is available. Only `AnySilo` is currently supported; `Wait` is rejected at startup until durable waiting is supported. This option does not change placement yet. |
| siloPort | number | 11111 | Port for silo to silo communication. Must differ per node when multiple nodes run on one machine. |
| gatewayPort | number | 30000 | Port for the silo's client gateway. Must differ per node when multiple nodes run on one machine. |
| clusterId | string | chronicle | The cluster id - all nodes that should form one cluster must share it. |
| serviceId | string | chronicle | The service id - all nodes that should form one cluster must share it. |
| advertisedIP | string | | The IP address the silo advertises to other cluster members. Resolved from the machine's host name when not set; set it explicitly (e.g. `127.0.0.1`) when running multiple nodes on one machine. |
| defunctSiloCleanupPeriod | timespan | 01:00:00 | How often defunct silo entries are swept out of the membership table when using `MongoDB` or `Sql` clustering. Without the sweep, dead entries from restarts and failed rollouts accumulate and slow down or block new nodes joining. Set to `00:00:00` to disable the sweep. |
| defunctSiloExpiration | timespan | 03:00:00 | The age at which a defunct membership entry is removed by the sweep. A node never reuses a silo identity, so dead entries only have diagnostic value. |
| probeTimeout | timespan | Orleans default | Interval between liveness probes and the timeout for each probe. Applies to `MongoDB` and `Sql` clustering. |
| numMissedProbesLimit | number | Orleans default | Missed probes before a silo is suspected to be down. Applies to `MongoDB` and `Sql` clustering. |
| numVotesForDeathDeclaration | number | Orleans default | Non-expired votes needed to declare a silo down. Must not exceed `numProbedSilos`. Applies to `MongoDB` and `Sql` clustering. |
| tableRefreshTimeout | timespan | Orleans default | Interval between fetching membership table updates. Applies to `MongoDB` and `Sql` clustering. |
| iAmAliveTablePublishTimeout | timespan | Orleans default | Interval between publishing this silo's heartbeat to the membership table. Applies to `MongoDB` and `Sql` clustering. |
| numProbedSilos | number | Orleans default | Number of other silos each silo probes for liveness. Applies to `MongoDB` and `Sql` clustering. |
| roles.eventSequences | boolean | true | When `true`, event sequence grains can be activated on this node. When `false`, event sequence grains will not be placed on this node. |
| roles.observers | boolean | true | When `true`, observer grains (reactors, reducers, projections) can be activated on this node. When `false`, observer grains will not be placed on this node. |

The optional membership settings above keep the Orleans defaults when omitted. Time spans use
`hh:mm:ss` format. Configure the same values on every node. Shorter timeouts detect failures sooner,
but can also mark healthy nodes as failed during network delays or heavy load. Orleans validates the
membership options at startup; these settings do not change localhost clustering.

## Configuration examples

### Default (all roles enabled)

By default, all roles are enabled on every node. This is the standard single-node or homogeneous multi-node configuration:

```json
{
  "clustering": {
    "roles": {
      "eventSequences": true,
      "observers": true
    }
  }
}
```

### Dedicated event sequence node

A node that only handles event sequences (event ingestion and appending):

```json
{
  "clustering": {
    "roles": {
      "eventSequences": true,
      "observers": false
    }
  }
}
```

### Dedicated observer node

A node that only processes observers (reactors, reducers, projections):

```json
{
  "clustering": {
    "roles": {
      "eventSequences": false,
      "observers": true
    }
  }
}
```

## Jobs role and metadata

All three roles (`eventSequences`, `observers` and `jobs`) default to `true`. Startup refuses a node
with all three disabled. Existing configurations that disable both older roles remain valid because
`jobs` defaults to `true`.

Set `clustering.roles.jobs` to `false` (environment variable
`Cratis__Chronicle__Clustering__Roles__Jobs=false`) to reduce that silo's concurrent job step budget.
This is a foundation for dedicated job workers, not placement enforcement: job steps can still run on
any compatible silo. Job controllers are unchanged. See [Job throttling](job-throttling.md) for the
reduced budget and its override.

Silos advertise version `1` under `cratis.chronicle.roles.v` and Boolean strings under
`cratis.chronicle.role.eventSequences`, `cratis.chronicle.role.observers` and
`cratis.chronicle.role.jobs` in Orleans silo metadata. Chronicle merges these namespaced keys with
host-provided metadata; unrelated keys are preserved. A silo without recognized, complete metadata
has unknown roles. Placement prefers silos known to have the required role over silos with unknown roles.

Chronicle enables Orleans silo metadata itself, so hosts should not call `UseSiloMetadata` after
`AddChronicleToSilo`. When the host has already enabled it before Chronicle, Chronicle only merges its keys.

During a rolling upgrade, silos on the new version log errors when fetching metadata from older silos.
This is expected and harmless: those silos' roles are read as unknown.

`clustering.jobsFallback` defaults to `AnySilo`. `Wait` is reserved and rejected at startup until
durable waiting support is available (Cratis/Orleans#129). Do not configure `Wait` yet.

## Behavior

Role-based placement is applied by custom Orleans placement directors for event sequence and observer
grains. The silo making the placement decision starts with the silos Orleans reports as compatible
with the grain type, and selects the first nonempty tier:

1. Silos known to have the required role.
2. Silos with unknown roles, including older silos without metadata.
3. No eligible silo: placement fails with `InvalidOperationException`.

Within the selected tier, placement is random. The local silo's roles come from its own configuration,
not its cached metadata. With the default configuration (all roles enabled on every silo), every
compatible silo remains eligible and random placement is unchanged.

Observer and event sequence placement never falls back to a silo known to lack the role. If every
compatible silo has the role disabled, the exception names the missing role and its configuration:

- `No silo has the Observers role or unknown roles for observer grain placement; enable Cratis:Chronicle:Clustering:Roles:Observers on at least one compatible silo.`
- `No silo has the EventSequences role or unknown roles for event sequence grain placement; enable Cratis:Chronicle:Clustering:Roles:EventSequences on at least one compatible silo.`

This is stricter than local-only placement: if all event sequence hosts are unavailable, appends can
fail rather than activate event sequence grains on a silo known to have that role disabled. Keep at
least one compatible host for each required role available.

The registered job worker director additionally permits silos known to lack `Jobs` as a last resort
with `jobsFallback: AnySilo`, and warns once on each transition into or out of that fallback. It is
not yet applied to job steps, so their placement is unchanged in this release. If no compatible silo
is eligible, its exception says `No compatible silo is eligible for the Jobs role; enable Cratis:Chronicle:Clustering:Roles:Jobs on at least one compatible silo.`

### Projection subscriber locality

Kernel projection subscribers use Orleans' built-in prefer-local placement. A cold partition normally
activates beside its live observer. Full projection replay and catch-up use one streaming observer
step; cold subscribers prefer that step's silo. These streaming steps leave subscriber activations to
Orleans' normal idle collection. The subscriber key is unchanged, including the subscription's silo
address. Client reactors and reducers retain their client-addressed routing.

Partition steps used for retries, failed-partition replay and per-partition catch-up request subscriber
deactivation after success, failure or cancellation. This release applies only to partitioned kernel
projection subscribers, not collapsing projections. Release is best effort: a failure is logged without
changing the step result. Orleans drains the activation before reactivation, allowing the next delivery
to choose locality again. Subscriber fields contain only rebuildable definitions, pipeline references
and caches; delivery is awaited before release. No durable checkpoint or sink state is discarded.

This is a preference, not a dedicated role or a migration of a warm activation. A collapsing projection
keeps its shared all-partitions activation warm and retains its existing serialization; it does not fan
out or release after each partition step. The built-in strategy needs no new server-side registration
during rolling upgrades. Complete the upgrade on all silos to get the new release behavior consistently.

### Rolling upgrades

Cluster-wide role enforcement requires every silo to run this release. Older silos retain their
local-only directors and can still place grains on remote silos with the role disabled. Silos without
metadata are treated as potentially having every role, but are selected only when no compatible silo
is known to have the required role. Already-active grains are not migrated.

Configure `jobsFallback` uniformly across the cluster: the deciding silo's configuration applies.
`JobWorkerPlacementStrategy` is registered ahead of use. Applying it to job steps must ship in a later
release, after every silo has the registration. Orleans throws for an unregistered strategy name;
it does not fall back to its default placement strategy. Keep a two-release gap before applying the
attribute. Clusters skipping the registration release must complete that upgrade before upgrading to
a release that applies the attribute; a version gap alone does not make an unregistered silo safe.

## Deploying a single node

With localhost clustering, including SQL deployments that have not opted into `Sql` clustering, the deployment must run one instance at a time. Stop the old instance before starting the new one. In Azure Container Apps, use single revision mode with no overlap between revisions. In Kubernetes, use the `Recreate` deployment strategy, or a rolling update with `maxSurge: 0`.

## Architecture

Chronicle uses custom Orleans placement strategies for role-based placement:

- **EventSequencePlacementStrategy**: applied to `EventSequence` grains and resolved by `EventSequencePlacementDirector`
- **ObserverPlacementStrategy**: applied to `Observer` grains (the base class for reactors, reducers, and projections) and resolved by `ObserverPlacementDirector`

- **JobWorkerPlacementStrategy**: registered with `JobWorkerPlacementDirector`, but not yet applied to job steps

The directors share `IRoleEligibility`, which uses the local `Clustering.Roles` configuration and
remote silo metadata to choose the highest available tier. Observer and event sequence directors
exclude known role-disabled silos; the job worker director allows them only as the `AnySilo` fallback.
All three select randomly within their eligible tier.
