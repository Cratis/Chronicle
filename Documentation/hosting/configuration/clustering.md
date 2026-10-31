---
description: Configure how multiple Chronicle Server nodes form one Orleans cluster and distribute workloads by role for horizontal scale out.
---
# Clustering

Clustering configuration controls how multiple Chronicle Server nodes form one Orleans cluster and how grain workloads are distributed across them. This enables horizontal scale out and assigning specific roles to different nodes—for example, dedicating some nodes to event sequences and others to observers (reactors, reducers, projections).

> [!WARNING]
> The clustering type defaults to `Localhost`, which means **one active node per kernel storage**,
> not one independent node per container. A single-writer lease guards this rule by default on MongoDB,
> PostgreSQL, SQL Server and SQLite. A second node waits without activating grains or processing events,
> then fails startup if its acquisition timeout expires. Changing `clusterId` or `serviceId` does not
> bypass this guard. `InMemory` is process-local and never takes a lease.
>
> A deployment with multiple active nodes must explicitly select `MongoDB` with MongoDB storage, or `Sql`
> with SQL Server or PostgreSQL storage, on every node. These modes use cluster membership rather than
> the single-writer lease. All nodes must share the same cluster database, `clusterId` and `serviceId`.
> Configure the type through `clustering.type` in `chronicle.json` or `Cratis__Chronicle__Clustering__Type`.
> Unsupported combinations fail at startup: `Sql` requires `MsSql` or `PostgreSql` storage, and `MongoDB`
> requires MongoDB storage. SQLite and in-memory storage do not support multi-node clustering.

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
    "gatewayEnabled": true,
    "clusterId": "chronicle",
    "serviceId": "chronicle",
    "roles": {
      "eventSequences": true,
      "observers": true
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
Membership uses Chronicle's SQL connection resolution and schema provisioning. Silo ports must be reachable between nodes, and enabled gateways must be reachable by trusted Orleans clients; use `advertisedIP` when the default address is not reachable.

The defunct-silo cleanup settings apply to both `Sql` and `MongoDB`: the sweep runs hourly by default
and removes defunct entries older than three hours. Set the cleanup period to `00:00:00` to disable it.
The development kernel state reset is refused under `Sql` clustering because it would erase live
membership. Use an isolated `Localhost` cluster for development resets. To replace the nodes of a `MongoDB` or `Sql` cluster, see the [rolling-deploy runbook](../rolling-deploy.md), which starts with which versions can run side by side.

After a node is lost, observers can lag behind the event log until the observer watchdog catches them up. This applies to every clustering type and is tracked in [Chronicle#5246](https://github.com/Cratis/Chronicle/issues/5246).

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| type | string | Localhost | Cluster membership. `Localhost` is single node development clustering. `MongoDB` keeps membership in MongoDB storage. `Sql` keeps membership in the SQL Server or PostgreSQL cluster database. Multi-node clustering is explicit opt-in; all nodes must share the cluster database and cluster identity. |
| siloPort | number | 11111 | Port for silo to silo communication. Must differ per node when multiple nodes run on one machine. |
| gatewayPort | number | 30000 | Port for the silo's client gateway when enabled. Must differ per node when multiple nodes run on one machine. |
| gatewayEnabled | boolean | true | Set to `false` to disable the Orleans client gateway on this node. HTTP and gRPC remain available. |
| clusterId | string | chronicle | The cluster id - all nodes that should form one cluster must share it. |
| serviceId | string | chronicle | The service id - all nodes that should form one cluster must share it. |
| advertisedIP | string | | In MongoDB and SQL clustering, the IP address advertised to other silos and bound by the silo and enabled gateway listeners. Must belong to a local interface and be reachable from peer nodes. When omitted, Orleans keeps its automatic address selection and listener defaults. Localhost clustering keeps its loopback address selection. |
| defunctSiloCleanupPeriod | timespan | 01:00:00 | How often defunct silo entries are swept out of the membership table when using `MongoDB` or `Sql` clustering. Without the sweep, dead entries from restarts and failed rollouts accumulate and slow down or block new nodes joining. Set to `00:00:00` to disable the sweep. |
| defunctSiloExpiration | timespan | 03:00:00 | The age at which a defunct membership entry is removed by the sweep. A node never reuses a silo identity, so dead entries only have diagnostic value. |
| probeTimeout | timespan | Orleans default | Interval between liveness probes and the timeout for each probe. Applies to `MongoDB` and `Sql` clustering. |
| numMissedProbesLimit | number | Orleans default | Missed probes before a silo is suspected to be down. Applies to `MongoDB` and `Sql` clustering. |
| numVotesForDeathDeclaration | number | Orleans default | Non-expired votes needed to declare a silo down. Must not exceed `numProbedSilos`. Applies to `MongoDB` and `Sql` clustering. |
| tableRefreshTimeout | timespan | Orleans default | Interval between fetching membership table updates. Applies to `MongoDB` and `Sql` clustering. |
| iAmAliveTablePublishTimeout | timespan | Orleans default | Interval between publishing this silo's heartbeat to the membership table. Applies to `MongoDB` and `Sql` clustering. |
| numProbedSilos | number | Orleans default | Number of other silos each silo probes for liveness. Applies to `MongoDB` and `Sql` clustering. |
| singleWriterLease.enabled | boolean or null | null (automatic) | Enables the lease for `Localhost` on persistent storage. Set `false` only when another mechanism guarantees one writer. Does not enable a lease for `MongoDB` or `Sql` clustering, or `InMemory` storage. |
| singleWriterLease.ttl | timespan | 00:00:30 | Database lease lifetime; minimum five seconds. |
| singleWriterLease.acquireTimeout | timespan | 00:02:00 | Maximum startup wait. Zero makes one acquisition attempt; negative values are invalid. |
| roles.eventSequences | boolean | true | When `true`, event sequence grains can be activated on this node. When `false`, event sequence grains will not be placed on this node. |
| roles.observers | boolean | true | When `true`, observer grains (reactors, reducers, projections) can be activated on this node. When `false`, observer grains will not be placed on this node. |

The optional membership settings above keep the Orleans defaults when omitted. Time spans use
`hh:mm:ss` format. Configure the same values on every node. Shorter timeouts detect failures sooner,
but can also mark healthy nodes as failed during network delays or heavy load. Orleans validates the
membership options at startup; these settings do not change localhost clustering.

## Single-writer lease

The node acquires the fixed `kernel` lease in the system/cluster database **before** Orleans runtime
initialization. No grains, observer rehydration or processing start until acquisition succeeds. Waiting
nodes are not ready. Acquisition waits at most `acquireTimeout`; the timeout error identifies the last
known holder, host and expiry. Temporary storage failures are retried within that budget.

The holder renews every TTL/3. If it cannot confirm a renewal for TTL/2 (15 seconds by default), it marks
readiness unhealthy, logs at Critical level and requests shutdown with a nonzero process exit code. A
watchdog forces process termination before the original TTL expires if graceful shutdown gets stuck.
A renewal rejected because ownership was lost fences the process immediately. Set the TTL so that half
of it comfortably exceeds the host's shutdown timeout. Your restart policy must restart failed processes.

During normal shutdown, renewal continues until the silo has stopped its grains and runtime services;
then the lease is released. After SIGKILL, a crash or a lost release, a successor waits at most the
remaining TTL rather than requiring manual cleanup. The lease row/document remains for diagnostics;
expiry is not driven by a MongoDB TTL index.

PostgreSQL, SQL Server and MongoDB use **database server time** for acquisition, renewal and expiry, not
the application's wall clock. MongoDB requires version 5.0 or later for server-time `$dateAdd` update pipelines.
SQLite uses the host clock: sharing a SQLite file across hosts with unsynchronized clocks is unsupported,
as are network shares without reliable file locking.

Disabling `singleWriterLease.enabled` preserves unguarded single-node behavior. If storage is remote,
the server warns about the risk, including for PostgreSQL and SQL Server. This is an escape hatch, not a
way to scale out. See [Production scaling](../production.md#scaling) for safe replacement deployments.

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

## Startup and probes

The kernel binds port 35000 and any dedicated health port before starting the Orleans silo. Use `tcpSocket` on port 35000 or `/health/live` for a startup probe, `/health/live` for liveness, and `/health` or `/health/ready` for readiness. Readiness waits for silo startup (including Chronicle's startup task) and host startup, and fails once shutdown begins. Background catch-up can continue after readiness succeeds.

An open TCP port is not readiness. Non-health requests receive HTTP 503 with `Retry-After` or gRPC `Unavailable` until ready. Raising `failureThreshold` is not the fix for backlog-dependent startup restarts: separate listener liveness from silo readiness. See [Health Endpoint](health-endpoint.md) for dedicated-port probe examples.

## Behavior

Role-based placement is applied by custom Orleans placement directors - one for event sequence grains and one for observer grains. When a grain needs to be activated, the director running on the silo that makes the placement decision:

1. Starts from the silos Orleans reports as compatible with the grain type.
2. Removes **this** silo from the candidates when this silo has the grain's role disabled.
3. Selects one of the remaining candidates at random.
4. Throws an `InvalidOperationException` when no candidate remains - for example, when every silo has that role disabled.

> [!NOTE]
> A placement director only sees the role configuration of the silo it runs on. It can keep a grain off the local silo when the local role is disabled, but it does not currently guarantee that a grain is never placed on a *remote* silo whose role is disabled. Role-based isolation is therefore best-effort today - treat the role settings as a scheduling hint rather than a hard cluster-wide guarantee, and run homogeneous nodes (all roles enabled, the default) when every node must accept every grain.

## Deploying a single node

With localhost clustering, including SQL deployments that have not opted into `Sql` clustering, the deployment must run one instance at a time. Stop the old instance before starting the new one. In Azure Container Apps, use single revision mode with no overlap between revisions. In Kubernetes, use the `Recreate` deployment strategy, or a rolling update with `maxSurge: 0`.

## Listener and network boundaries

The defaults are unchanged: the gateway is enabled, and omitting `advertisedIP` leaves Orleans' address selection and listener defaults intact. For MongoDB or SQL clustering, setting `advertisedIP` explicitly binds the silo and enabled gateway to that address. Use a private interface or pod IP, not a public NAT address absent from the host. Peers must be able to route to it. Localhost mode retains its existing loopback selection.

If applications use Chronicle's HTTP or gRPC APIs rather than an Orleans client, disable the Orleans gateway on every applicable node:

```json
{
  "clustering": {
    "gatewayEnabled": false
  }
}
```

The environment-variable equivalent is `Cratis__Chronicle__Clustering__GatewayEnabled=false`. The configured `gatewayPort` is retained for later re-enabling, but Orleans receives port `0` while disabled. The silo port remains necessary for multi-node clusters. Changing either setting requires a server restart; mixed-version nodes can coexist, but older nodes do not honor `gatewayEnabled`.

The silo protocol on port 11111 and the Orleans client protocol on port 30000 do not use Chronicle's HTTP authentication boundary. Restrict them to trusted cluster peers and, only if needed, trusted Orleans clients. Do not publish these ports on an internet-facing load balancer, ingress, or host interface. Disabling the gateway does not protect the silo port. Listener binding and firewall policies are not transport encryption or peer authentication.

For Docker Compose, publish only the required HTTP/gRPC and health ports. Keep the silo and any enabled gateway on a dedicated private Docker network shared only by trusted nodes. Leaving a port out of `ports` prevents host publication, but does not isolate containers sharing the network. See [Docker Compose](../docker-compose.md#private-cluster-ports).

### Kubernetes ingress policy example

This example selects Chronicle pods labeled `app: chronicle` in the policy's namespace. It allows silo traffic only from other Chronicle pods in that namespace, and API traffic only from namespaces labeled `chronicle-clients: "true"`. It deliberately does not allow the Orleans gateway; set `gatewayEnabled` to `false` when it is not needed. Adapt selectors and configured ports to your deployment, including any ingress controller and health probes. If an Orleans client needs the gateway, add a separate narrow allow rule for its trusted pods on the configured gateway port.

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: chronicle-ingress
spec:
  podSelector:
    matchLabels:
      app: chronicle
  policyTypes:
    - Ingress
  ingress:
    - from:
        - podSelector:
            matchLabels:
              app: chronicle
      ports:
        - protocol: TCP
          port: 11111
    - from:
        - namespaceSelector:
            matchLabels:
              chronicle-clients: "true"
      ports:
        - protocol: TCP
          port: 35000
```

A NetworkPolicy-capable CNI is required. Policies are additive: another allow-all policy can broaden access. This ingress-only example does not restrict egress or configure access to storage, DNS, or other dependencies.

## Architecture

Chronicle uses custom Orleans placement strategies for role-based placement:

- **EventSequencePlacementStrategy**: applied to `EventSequence` grains and resolved by `EventSequencePlacementDirector`
- **ObserverPlacementStrategy**: applied to `Observer` grains (the base class for reactors, reducers, and projections) and resolved by `ObserverPlacementDirector`

Each director consults the `Clustering.Roles` configuration of the silo it runs on and selects a compatible node using random placement, excluding the local silo when its own role for that grain type is disabled.
