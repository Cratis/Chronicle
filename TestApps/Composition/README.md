# Scale-out composition

An Aspire composition for verifying Chronicle's scale-out capabilities end to end on one machine:

| Resource | What it is | Where |
|---|---|---|
| `mongodb` | `cratis/mongodb` (single node replica set) - storage AND Orleans cluster membership | `localhost:27019` |
| `dns` | CoreDNS serving the `chronicle.local` zone with `_chronicle._tcp` SRV records pointing at both kernels | `localhost:8053/udp` |
| `kernel-1` / `kernel-2` | Two Chronicle server processes forming ONE Orleans cluster (MongoDB membership) - each already hosts its own Workbench (UI + API) | `localhost:35001` / `localhost:35002` |
| `app-1` / `app-2` | Two instances of the AspNetCore test app, connecting with `chronicle+srv://chronicle.local` resolved through the CoreDNS container | <http://localhost:5101> / <http://localhost:5102> |
| `workbench` | A YARP reverse proxy round-robining across `kernel-1` and `kernel-2`'s own embedded Workbench - not a separate host | <http://localhost:9876> |

Every Chronicle Server already serves its own Workbench (static UI + full API) on its main port -
that isn't something a composition adds, it's what a single kernel does by default. So "one
load-balanced Workbench" doesn't mean standing up a third app that round-robins its own connection
to the cluster; it means putting a reverse proxy in front of the two kernels' identical,
cluster-backed Workbench endpoints. `kernel-1` and `kernel-2` back one Orleans cluster and one
MongoDB storage, so either one's Workbench shows the same data - YARP just picks which one answers
each request. Each kernel's dev TLS certificate is self-signed per-process, so the YARP cluster is
configured with `DangerousAcceptAnyServerCertificate` rather than validating against a CA.

Plain round-robin isn't enough on its own, though: the Workbench's observable queries open an SSE
stream to get a connectionId, then POST to `/subscribe` with that id as a *separate* request. If
that POST round-robins to the other kernel, it doesn't recognize the connectionId and the query
fails and reconnects forever. The YARP cluster is configured with cookie-based session affinity so
a browser session sticks to the kernel that issued its connectionId for its whole lifetime - a
fresh session (or a failover) still picks a kernel via round-robin.

## Run it

```shell
dotnet run --project TestApps/Composition
```

## What to verify

- **DNS SRV discovery**: the web apps' connection string is
  `chronicle+srv://...@chronicle.local/?srvNameServer=127.0.0.1:8053` - they find the kernels
  purely through the SRV records (`dig -p 8053 @127.0.0.1 _chronicle._tcp.chronicle.local SRV`).
- **Least-connections spread**: app-1 and app-2 each ask both kernels how many clients they
  already have (`GET /connections/count`) before connecting, so they reliably land on different
  silos instead of leaving it to a coin flip - check via Workbench's Connected Clients page or
  `curl -k https://localhost:35001/connections/count`.
- **Client fan out**: open both web apps (5101 and 5102), act on different employees from either
  page, and watch the "Reactor invocations on this instance" section - each employee (partition)
  is sticky to one of the two instances, spread round-robin by partition key.
- **Constraints across the cluster**: "Steal email" is rejected by the unique email constraint
  regardless of which app or kernel handles it.
- **Connected clients**: open an event store, then System > Connected Clients, to see every
  connected client and which kernel (silo) terminates its connection - flat or grouped by silo.
- **Load-balanced Workbench**: hit <http://localhost:9876> repeatedly (or refresh) - YARP
  round-robins each request across `kernel-1` and `kernel-2`'s own embedded Workbench, and both
  show the same cluster-wide data since they share one Orleans cluster and one MongoDB storage.
