---
title: Run Chronicle on Kubernetes
description: Deploy the Cratis Chronicle production image to Kubernetes with a tested reference manifest, probes, a PodDisruptionBudget, a NetworkPolicy and secret handling.
---

Run the Chronicle Kernel on Kubernetes from the manifest on this page: a StatefulSet with probes matched to how the kernel starts, a PodDisruptionBudget, spread across nodes, a locked-down security context and a NetworkPolicy. The manifest was applied to a local three-node [kind](https://kind.sigs.k8s.io/) cluster (one control plane and two workers, Kubernetes 1.37, the kindnet network plugin, zones labeled by hand) with the `19.153.2` production image: both kernels became ready, and a node drain evicted one kernel, kept the other serving and rescheduled the evicted one once a node was free. Managed Kubernetes services, other network plugins and real zones were not part of that test.

The manifest needs Kubernetes 1.30 or later. Its `preStop` hook uses the `sleep` action, which is on by default from 1.30 and generally available from 1.34. Older API servers reject the manifest, and the production image has no shell to run an `exec` sleep instead.

Read [Production hosting](production.md) first. This page assumes the storage, TLS and encryption-certificate requirements described there, and adds what is specific to Kubernetes.

## Choose a StatefulSet

Use a StatefulSet, even though the kernel keeps all of its state in the storage backend and needs no persistent volume:

- **Stop before start.** A StatefulSet never runs two pods with the same name. On a rolling update or an eviction it stops the old pod before it creates the replacement. A single kernel with the default `Localhost` clustering needs exactly that: the replacement waits for the [single-writer lease](configuration/clustering.md#single-writer-lease), which the old kernel releases when it stops.
- **One node at a time.** A rolling update replaces one pod and waits for it to become ready before it moves to the next, which is how [Rolling deploy](rolling-deploy.md) asks you to replace nodes.
- **Stable names.** Pod names such as `chronicle-0` appear as the host name in the cluster membership table and in logs, so a restarted kernel is easy to recognize.

A Deployment also works. With more than one kernel, use `MongoDB` or `Sql` clustering and the default rolling update. With a single `Localhost` kernel, set `strategy: Recreate`, or `RollingUpdate` with `maxSurge: 0` and `maxUnavailable: 1`. The default one-replica Deployment rollout (`maxSurge: 1`) never converges on its own: the new pod waits for the lease while Kubernetes waits for it to become ready. It does converge with `Cratis__Chronicle__Clustering__SingleWriterLease__Standby=true`, because the new pod then reports ready in standby while it waits; see [Standby for start-before-stop rollouts](configuration/clustering.md#standby-for-start-before-stop-rollouts). Standby gives up readiness as a rollout gate, so validate images before rolling them out and keep the previous image ready to roll back to.

## Reference manifest

The manifest runs two kernels as one cluster with MongoDB clustering. Lines marked `Your ...` are placeholders; everything else is what was tested. It expects two Secrets, `chronicle-storage` and `chronicle-certificates`, described under [Secrets](#secrets).

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: chronicle
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: chronicle
  namespace: chronicle
automountServiceAccountToken: false
---
# Headless service: gives each kernel a stable DNS name and lets the StatefulSet own its pods.
apiVersion: v1
kind: Service
metadata:
  name: chronicle-cluster
  namespace: chronicle
spec:
  clusterIP: None
  publishNotReadyAddresses: true
  selector:
    app: chronicle
  ports:
    - name: silo
      port: 11111
---
# Client-facing service: gRPC, REST API and Workbench on port 35000. Only ready kernels receive traffic.
apiVersion: v1
kind: Service
metadata:
  name: chronicle
  namespace: chronicle
spec:
  selector:
    app: chronicle
  ports:
    - name: chronicle
      port: 35000
      targetPort: 35000
---
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: chronicle
  namespace: chronicle
spec:
  serviceName: chronicle-cluster
  replicas: 2
  podManagementPolicy: Parallel
  updateStrategy:
    type: RollingUpdate
  selector:
    matchLabels:
      app: chronicle
  template:
    metadata:
      labels:
        app: chronicle
    spec:
      serviceAccountName: chronicle
      automountServiceAccountToken: false
      terminationGracePeriodSeconds: 30
      securityContext:
        runAsNonRoot: true
        runAsUser: 1654
        runAsGroup: 1654
        fsGroup: 1654
        seccompProfile:
          type: RuntimeDefault
      affinity:
        podAntiAffinity:
          requiredDuringSchedulingIgnoredDuringExecution:
            - labelSelector:
                matchLabels:
                  app: chronicle
              topologyKey: kubernetes.io/hostname
      topologySpreadConstraints:
        - maxSkew: 1
          topologyKey: topology.kubernetes.io/zone
          whenUnsatisfiable: ScheduleAnyway
          labelSelector:
            matchLabels:
              app: chronicle
      containers:
        - name: chronicle
          image: cratis/chronicle:19.153.2 # Your tested release: pin it
          ports:
            - name: chronicle
              containerPort: 35000
            - name: silo
              containerPort: 11111
            - name: health
              containerPort: 8080
          env:
            - name: Cratis__Chronicle__Storage__Type
              value: MongoDB
            - name: Cratis__Chronicle__Storage__ConnectionDetailsFile
              value: /var/run/secrets/chronicle/storage/connection-string
            - name: Cratis__Chronicle__Clustering__Type
              value: MongoDB
            - name: Cratis__Chronicle__Clustering__ClusterId
              value: chronicle
            - name: Cratis__Chronicle__Clustering__ServiceId
              value: chronicle
            - name: Cratis__Chronicle__Clustering__GatewayEnabled
              value: "false"
            - name: POD_IP
              valueFrom:
                fieldRef:
                  fieldPath: status.podIP
            - name: Cratis__Chronicle__Clustering__AdvertisedIP
              value: $(POD_IP)
            - name: Cratis__Chronicle__Tls__CertificatePath
              value: /var/run/secrets/chronicle/certificates/tls.pfx
            - name: Cratis__Chronicle__Tls__CertificatePassword
              valueFrom:
                secretKeyRef:
                  name: chronicle-certificates
                  key: tls-password
            - name: Cratis__Chronicle__EncryptionCertificate__CertificatePath
              value: /var/run/secrets/chronicle/certificates/encryption.pfx
            - name: Cratis__Chronicle__EncryptionCertificate__CertificatePassword
              valueFrom:
                secretKeyRef:
                  name: chronicle-certificates
                  key: encryption-password
            - name: Cratis__Chronicle__Health__Port
              value: "8080"
            - name: Cratis__Chronicle__Health__Tls
              value: "false"
            - name: Cratis__Chronicle__Health__Exclusive
              value: "true"
          startupProbe:
            tcpSocket:
              port: 35000
            periodSeconds: 10
            failureThreshold: 30
          livenessProbe:
            httpGet:
              path: /health/live
              port: health
            periodSeconds: 10
            timeoutSeconds: 5
            failureThreshold: 6
          readinessProbe:
            httpGet:
              path: /health
              port: health
            periodSeconds: 5
            timeoutSeconds: 5
            failureThreshold: 3
          lifecycle:
            preStop:
              sleep:
                seconds: 5
          resources: # Your measured sizes: see Container resources
            requests:
              cpu: 500m
              memory: 1Gi
            limits:
              memory: 2Gi
          securityContext:
            allowPrivilegeEscalation: false
            readOnlyRootFilesystem: true
            capabilities:
              drop: ["ALL"]
          volumeMounts:
            - name: tmp
              mountPath: /tmp
            - name: storage
              mountPath: /var/run/secrets/chronicle/storage
              readOnly: true
            - name: certificates
              mountPath: /var/run/secrets/chronicle/certificates
              readOnly: true
      volumes:
        - name: tmp
          emptyDir:
            sizeLimit: 256Mi
        - name: storage
          secret:
            secretName: chronicle-storage
            defaultMode: 0440
        - name: certificates
          secret:
            secretName: chronicle-certificates
            defaultMode: 0440
---
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: chronicle
  namespace: chronicle
spec:
  maxUnavailable: 1
  selector:
    matchLabels:
      app: chronicle
```

### Clustering and ports

Every kernel has the same `ClusterId`, `ServiceId` and clustering type, which has to match the storage: `MongoDB` here, `Sql` for PostgreSQL or SQL Server. The manifest sets `ClusterId` and `ServiceId` to their default, `chronicle`, to make them visible. They have to be identical on every kernel of one cluster, and different for every cluster that shares the same database, so change both when you run a second cluster against the same MongoDB. `AdvertisedIP` takes the pod IP from the downward API, so each silo binds and advertises the address its peers can route to.

The manifest turns the Orleans gateway (port 30000) off, because applications that use the Chronicle clients talk to port 35000. Keep it on only for Orleans clients, and add the [gateway rule](#allow-the-orleans-gateway) to the NetworkPolicy. The membership table then shows a gateway port of 0 for each silo.

| Port | Purpose | Reached by |
| --- | --- | --- |
| 35000 | gRPC, REST API, Workbench and OAuth over TLS | Applications, your ingress |
| 11111 | Orleans silo-to-silo protocol | Other kernels only |
| 30000 | Orleans gateway, off in this manifest | Trusted Orleans clients only |
| 8080 | Dedicated health port, plaintext HTTP/1.1 | The kubelet's probes |

The image also sets `ASPNETCORE_HTTP_PORTS=8080`. The kernel configures its listeners itself and logs that it overrides that address, so the variable does not open a second listener. Port 8080 here is the health port from `Health__Port`.

`Health__Exclusive=true` makes port 8080 answer only `/health`, `/health/live` and `/health/ready`, and 404 for everything else. Without it the plaintext health port is a second, unencrypted way into the Workbench and the API. Route your ingress for the Workbench to port 35000, as [Workbench configuration](configuration/workbench.md#with-an-exclusive-health-port) shows. With TLS on, the port alone is not enough: the ingress has to connect to the kernel over HTTPS, or gRPC over TLS for gRPC routes (in ingress-nginx, the `backend-protocol` annotation set to `HTTPS` or `GRPCS`), and trust the kernel's certificate. [Reverse proxy examples](configuration/workbench.md#reverse-proxy-examples) covers the certificate trust, and [Trusted reverse proxies](configuration/tls.md#trusted-reverse-proxies) the forwarded headers from an ingress pod.

### Probes

The kernel binds its listeners before it starts the Orleans silo, so each probe checks a different thing:

| Probe | Target | What it checks |
| --- | --- | --- |
| Startup | TCP on 35000 | The process is up and has bound its listeners |
| Liveness | `GET /health/live` on 8080 | The process answers HTTP. It runs no health checks and stays successful while the silo starts |
| Readiness | `GET /health` on 8080 | The silo and Chronicle's startup work have completed, the storage checks pass and shutdown has not begun |

Readiness keeps a starting kernel out of the `chronicle` Service without restarting it. Never use the readiness endpoint, or a TCP check, as the liveness probe: a kernel that is still catching up would be killed and start over. `/health/ready` answers the same as `/health`. [Health Endpoint](configuration/health-endpoint.md) describes every check that contributes to readiness.

### Stopping

When Kubernetes stops a pod, it removes the pod from the Service endpoints and runs the `preStop` hook at the same time; `SIGTERM` follows when the hook returns. The five-second sleep keeps the kernel serving while load balancers and kube-proxy catch up. The `sleep` action runs inside the kubelet, so it works in the production image, which has no shell and no `sleep` binary.

The kernel then has 10 seconds to stop in an orderly way: its host shutdown timeout is fixed at 10 seconds, and the shutdown drain inside it defaults to 8 seconds. `terminationGracePeriodSeconds` covers the `preStop` sleep and that shutdown, so 30 seconds leaves room for both. Keep it at 30 or more. In the test a drained kernel left the cluster in an orderly way within six seconds of the eviction, and its membership row turned `Dead` rather than waiting to be suspected. See [Graceful shutdown](configuration/root-properties.md#graceful-shutdown).

### Disruptions and placement

The PodDisruptionBudget lets a drain, a cluster upgrade or the autoscaler evict one kernel at a time. In the test, draining the node of `chronicle-0` succeeded at once; draining the second node while `chronicle-0` was still unavailable was refused with `Cannot evict pod as it would violate the pod's disruption budget`, so the last kernel kept serving.

Required pod anti-affinity keeps two kernels off the same node, so a node failure takes down at most one. It also means an evicted kernel needs a free node: with two kernels on two worker nodes, the evicted kernel stays `Pending` until its node comes back. Have at least one node more than kernels if the cluster has to stay at full size during maintenance, or use `preferredDuringSchedulingIgnoredDuringExecution` to accept two kernels on one node for a while. The topology spread constraint additionally spreads the kernels over zones where the cluster has them, without blocking scheduling when it does not.

With a single kernel, keep `maxUnavailable: 1`. Eviction then costs a short outage while the kernel restarts elsewhere; in the test it was ready on the other node 11 seconds after the drain finished. Do not use `minAvailable: 1` with one replica: no drain could ever evict the pod.

A node that fails, rather than being drained, is different. Because a StatefulSet stops before it starts, it does not replace a pod on a node that stopped responding until Kubernetes knows the pod is gone: someone deletes the node object, force-deletes the pod with `kubectl -n chronicle delete pod chronicle-0 --grace-period=0 --force`, or adds the `node.kubernetes.io/out-of-service` taint to the node. With one `Localhost` kernel that is an outage until an operator acts. After a forced delete, the replacement waits up to the remaining [single-writer lease](configuration/clustering.md#single-writer-lease) TTL, because the old kernel never released the lease.

### Security context

The production image runs as the non-root user `1654` from `/app`, on a chiseled base image without a shell. The manifest makes that explicit and locks the rest down:

- `runAsNonRoot`, `runAsUser: 1654` and `runAsGroup: 1654` match the image's user. `fsGroup: 1654` lets that group read the Secret files, which are mounted with mode `0440`.
- `readOnlyRootFilesystem: true`. The kernel writes nothing under `/app`; the Data Protection key ring is stored in the storage backend, not on disk, so it needs no volume.
- An `emptyDir` on `/tmp`. The .NET runtime creates its diagnostics socket and debugger pipes there; `dotnet-counters`, `dotnet-trace` and `dotnet-dump` need them to attach to the kernel. Its 256Mi `sizeLimit` covers the socket and the pipes, not dumps. A full dump is about as large as the kernel's memory, up to the 2Gi limit, and the kubelet evicts a pod whose `emptyDir` grows past its `sizeLimit`, so a dump written to `/tmp` would kill the kernel you are diagnosing. If you expect to take dumps, add a separate volume larger than the memory limit in advance, for example a second `emptyDir` mounted at `/dumps`, and pass a path on it to `dotnet-dump collect --output`. [Collect crash dumps](#collect-crash-dumps) sets one up.
- No privilege escalation, every capability dropped, the runtime's default seccomp profile, and no service-account token, because the kernel does not call the Kubernetes API.

### Resources

Requests and limits depend on your workload. Memory follows the number of resident observers, reactors and reducers, which the [`cratis.chronicle.observer.resident`](configuration/open-telemetry.md#observer-state-jobs-and-append-latency) gauge reports per kernel, and CPU peaks during a cold start. Set them from your own measurements as described in [Container resources](container-resources.md). The manifest sets no CPU limit, so a kernel that is starting can use idle CPU on its node. If your cluster enforces one anyway, for example through a LimitRange that adds a default limit, set it from the cold-start peak as [Container resources](container-resources.md#cpu-is-a-cold-start-concern) describes.

## Budgets for a cold start

A kernel that starts against a large backlog is busy for a while: it reactivates observers, catches up and fills its caches. Because the kernel binds its listeners first, the startup probe passes within seconds, even before that work starts. The 300-second startup budget (`failureThreshold: 30` × `periodSeconds: 10`) only covers a process that is slow to start at all.

During the cold start, readiness keeps the kernel out of the Service for as long as it needs; it never restarts the pod. Liveness is what can restart it. `/health/live` runs no checks, but on a CPU-starved kernel even that request can wait in the thread pool. The manifest allows six failures 10 seconds apart, each with a 5-second timeout, so about a minute without an answer, before the kubelet restarts the kernel. Do not tighten that. A restart throws away the work done so far and starts the next cold start from the beginning.

Two things a probe budget cannot fix:

- **Starved CPU.** A CPU limit below the cold-start peak starves the thread pool, and a kernel that cannot complete a startup step stops itself and is restarted. Size CPU from the peak, as [Container resources](container-resources.md#cpu-is-a-cold-start-concern) describes.
- **Startup failures.** A startup step that exhausts its retries stops the kernel. Raising `failureThreshold` does not change that. [Know what a starting node does](rolling-deploy.md#know-what-a-starting-node-does) explains the startup steps and their retries, and [Handle a node that does not come up](rolling-deploy.md#6-handle-a-node-that-does-not-come-up) how to read their failures.

Two settings shape a saturated cold start. [`startup.busySiloWaitLimit`](configuration/root-properties.md#busy-silos-at-startup) (`Cratis__Chronicle__Startup__BusySiloWaitLimit`, two hours by default) bounds how long a startup step waits for a kernel that is up but too busy to answer before the kernel stops. [`observers.maxConcurrentSubscriptions`](configuration/observers.md#subscription-concurrency) (`Cratis__Chronicle__Observers__MaxConcurrentSubscriptions`) limits how many projection subscriptions and recovery batches a kernel runs at once, by default from its processor count.

## NetworkPolicy

These policies allow silo traffic only between kernels, API traffic only from namespaces labeled `chronicle-clients: "true"`, and outbound traffic from the kernel only to DNS, the other kernels and storage. They extend the [ingress example in Clustering](configuration/clustering.md#kubernetes-ingress-policy-example) with egress and with the client side.

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: chronicle
  namespace: chronicle
spec:
  podSelector:
    matchLabels:
      app: chronicle
  policyTypes:
    - Ingress
    - Egress
  ingress:
    # Silo-to-silo traffic between kernels only.
    - from:
        - podSelector:
            matchLabels:
              app: chronicle
      ports:
        - protocol: TCP
          port: 11111
    # gRPC, REST API and Workbench from client namespaces.
    - from:
        - namespaceSelector:
            matchLabels:
              chronicle-clients: "true"
      ports:
        - protocol: TCP
          port: 35000
  egress:
    # Name resolution.
    - to:
        - namespaceSelector:
            matchLabels:
              kubernetes.io/metadata.name: kube-system
          podSelector:
            matchLabels:
              k8s-app: kube-dns
      ports:
        - protocol: UDP
          port: 53
        - protocol: TCP
          port: 53
    # Silo-to-silo traffic between kernels only.
    - to:
        - podSelector:
            matchLabels:
              app: chronicle
      ports:
        - protocol: TCP
          port: 11111
    # Your storage: replace the selector, for example with an ipBlock for a managed database.
    - to:
        - podSelector:
            matchLabels:
              app: mongo
      ports:
        - protocol: TCP
          port: 27017
---
# Client side: lets the pods in an application namespace reach the kernel.
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: chronicle-client
  namespace: my-application # Your application namespace, labeled chronicle-clients: "true"
spec:
  podSelector: {}
  policyTypes:
    - Egress
  egress:
    - to:
        - namespaceSelector:
            matchLabels:
              kubernetes.io/metadata.name: chronicle
          podSelector:
            matchLabels:
              app: chronicle
      ports:
        - protocol: TCP
          port: 35000
    - to:
        - namespaceSelector:
            matchLabels:
              kubernetes.io/metadata.name: kube-system
          podSelector:
            matchLabels:
              k8s-app: kube-dns
      ports:
        - protocol: UDP
          port: 53
        - protocol: TCP
          port: 53
```

In the test, a pod in the labeled namespace reached `/health` on port 35000, and nothing else; a pod in an unlabeled namespace reached none of the kernel's ports.

Adapt the policies before you apply them:

- **Your network plugin has to enforce NetworkPolicy.** Without one, the policies are accepted and do nothing. Policies add up, so another policy that allows everything widens these.
- **Your ingress.** If an ingress controller publishes the Workbench or the API, allow its namespace on port 35000.
- **Your cluster DNS.** The DNS rules allow the pods labeled `k8s-app: kube-dns` in `kube-system`. Match them to how your cluster resolves names. With NodeLocal DNSCache, pods query a link-local address such as `169.254.20.10` that no pod selector matches, so allow it with an `ipBlock`; OpenShift runs its DNS in the `openshift-dns` namespace with other labels. A kernel that cannot resolve its storage host never becomes ready.
- **The health port.** The kubelet sends the probes from the node. Most network plugins let node traffic reach local pods regardless of policy, and the test cluster did. If your plugin filters it and the probes start failing, allow the node address range on port 8080 with an `ipBlock`. Do not open port 8080 to other namespaces.
- **More outbound traffic.** The egress rules cover DNS, the other kernels and MongoDB only. Add rules for what your configuration uses: an external authentication authority, an OpenTelemetry collector, webhook and sink targets, federation peers, or a compliance key store.
- **The client side.** The `chronicle-client` policy selects every pod in the application namespace and limits its outbound traffic to the kernel and DNS. Merge its rules into your application's own egress policy instead of applying it as it is.

### Allow the Orleans gateway

When an Orleans client needs the gateway, set `Cratis__Chronicle__Clustering__GatewayEnabled` to `true` and allow only that client's pods on port 30000:

```yaml
    - from:
        - namespaceSelector:
            matchLabels:
              chronicle-orleans-clients: "true"
      ports:
        - protocol: TCP
          port: 30000
```

Neither Orleans port uses Chronicle's authentication, and neither is encrypted. A NetworkPolicy restricts who can connect; it does not encrypt or authenticate the traffic.

## Running with TLS and authentication off

Some deployments terminate TLS at an ingress and run the kernel with `Cratis__Chronicle__Tls__Enabled=false`, often together with `Cratis__Chronicle__Authentication__Enabled=false`. The probes are unchanged, because the health port is plaintext HTTP/1.1 either way. In the test only those two settings changed; the TLS certificate entries stayed in the manifest and the Secret.

To run without a TLS certificate, also delete the `Cratis__Chronicle__Tls__CertificatePath` and `Cratis__Chronicle__Tls__CertificatePassword` entries from the manifest, and then the `tls.pfx` and `tls-password` keys from the `chronicle-certificates` Secret. Removing the keys while the `secretKeyRef` is still in the manifest leaves the pods in `CreateContainerConfigError`. Keep the encryption-certificate entries and keys. With TLS off and `Health__Tls=false`, the kernel does not load the TLS certificate, but this reduced manifest was not part of the test.

The main port changes:

- **Port 35000 speaks cleartext HTTP/2 (h2c) only.** HTTP/1.1 requests get `400`. The ingress or proxy has to forward over h2c, and browsers and clients use the proxy's HTTPS address. See [TLS configuration](configuration/tls.md) and [Workbench configuration](configuration/workbench.md#with-an-exclusive-health-port).
- **Authentication with TLS off needs an external HTTPS authority.** The kernel refuses to start with authentication on, TLS off and no HTTPS `Authentication:Authority`, because the internal authority cannot work out its public address behind the proxy.
- **Authentication off means anyone who can connect has full access.** In the test, a pod in an allowed namespace listed the event stores with a plain h2c request and no credentials. Every event, read model and management operation is open to every pod the NetworkPolicy lets in.

What remains after you accept that:

- The NetworkPolicy is the only access control. Restrict port 35000 to the ingress and the namespaces that run your own clients, and verify that your network plugin enforces it.
- Traffic between the proxy and the kernel, and between kernels, is unencrypted inside the cluster network.
- Data Protection still needs an encryption certificate to encrypt its keys at rest. Without one the kernel writes them unencrypted to storage.

[Turning authentication off](configuration/authentication.md#turning-authentication-off) explains why that setting is meant for kernels that no network can reach.

## Recreate or rolling update

| Situation | How to replace the kernels |
| --- | --- |
| One kernel, `Localhost` clustering | StatefulSet with one replica and `RollingUpdate`: it stops the old pod first. For a Deployment, use `Recreate`. Never use a Deployment's default rolling update without [standby](configuration/clustering.md#standby-for-start-before-stop-rollouts) |
| Several kernels, `MongoDB` or `Sql` clustering, both releases can share a cluster | Rolling update, one kernel at a time, waiting for readiness |
| An upgrade the [compatibility table](rolling-deploy.md#check-that-the-two-versions-can-share-a-cluster) or the [supported source releases](rolling-deploy.md#supported-source-releases) mark as stop-all, such as a new major or a release with a compatibility exception | Stop every kernel, change the image, then start the new release: `kubectl -n chronicle scale statefulset chronicle --replicas=0`, wait until the pods are gone, update the image with `kubectl -n chronicle set image statefulset/chronicle chronicle=<new image>`, then scale back up. Applying the manifest with the new image both updates it and restores the replica count. Changing the image while kernels still run starts the rolling update this row avoids. For a Deployment, use `Recreate` for that upgrade |
| A configuration change on the same release | The same strategy as for an upgrade between compatible releases |

Stopping every kernel first is always safe. What fails across a compatibility exception is the overlap of an old and a new kernel in one cluster, and a rolling update creates exactly that overlap. A new kernel that finds an incompatible release in the cluster logs a critical message and stops, and the rollout stalls with the old kernels still running.

Reverting the image does not unstick a stalled StatefulSet rollout on its own: the StatefulSet waits for the pod that never became ready before it replaces that pod again. Revert the template, then delete that pod so the StatefulSet recreates it from the reverted template, as the Kubernetes [forced rollback](https://kubernetes.io/docs/concepts/workloads/controllers/statefulset/#forced-rollback) notes describe. [Handle a node that does not come up](rolling-deploy.md#6-handle-a-node-that-does-not-come-up) explains how to read why the kernel stopped.

To run one kernel with `Localhost` clustering, change two lines of the manifest: set `replicas` to `1`, and `Cratis__Chronicle__Clustering__Type` to `Localhost`. That is the single-kernel variant the test ran; the other clustering entries stayed as they are. Keep the StatefulSet's `RollingUpdate`, which replaces the one pod stop-before-start.

## Secrets

The manifest reads two Secrets. Create them from your secret store, for example through a secrets operator or the CSI secrets-store driver; the names and keys below are what the manifest expects.

| Secret | Key | Used for |
| --- | --- | --- |
| `chronicle-storage` | `connection-string` | The storage connection string, read from a file through `Storage__ConnectionDetailsFile` |
| `chronicle-certificates` | `tls.pfx` | The TLS certificate for port 35000 |
| `chronicle-certificates` | `tls-password` | Its password, passed as `Tls__CertificatePassword` |
| `chronicle-certificates` | `encryption.pfx` | The encryption certificate |
| `chronicle-certificates` | `encryption-password` | Its password, passed as `EncryptionCertificate__CertificatePassword` |
| `chronicle-admin` (optional) | `password` | The initial administrator password, passed as `Authentication__AdminUser__Password` until setup is done |

- **Storage connection.** Mount the connection string as a file rather than an environment variable, so it does not show up in the pod's environment. The kernel reads the file once at startup and trims a trailing newline. Setting both `ConnectionDetails` and `ConnectionDetailsFile` stops startup. See [Read the connection from a file](configuration/storage.mdx#read-the-connection-from-a-file).
- **TLS certificate.** Include every name clients use in the certificate's subject alternative names, such as `chronicle.chronicle.svc.cluster.local`, and make clients validate it with `skipTlsValidation=false`. See [Client TLS](../configuration/tls.mdx) and [Server connection strings](../connection-strings/server.md).
- **Encryption certificate.** It protects the Data Protection keys and webhook credentials, and the internal OAuth authority signs and encrypts its tokens with it, so it has to be an RSA certificate with its private key. If it has a key-usage extension, include digital signature as well as key and data encipherment; without digital signature the kernel stops at startup with `The specified certificate is not a signing certificate.` Give every kernel the same certificate and the same `previous` list. See [Data Protection key encryption](encryption-certificate.md).
- **Data Protection keys.** They live in the storage backend, encrypted with the encryption certificate, so no kernel needs a key volume. A database backup carries the key ring; it does not carry the certificate that unlocks it.
- **Certificate passwords.** The kernel reads them from configuration, so they arrive through `secretKeyRef` environment variables. Write them into the Secret without a trailing newline, as the test did, because an environment variable keeps it. The pod specification holds only the reference, but anyone who can run commands in the pod or read the Secret can read the value, so limit both.
- **Rotating certificates.** The kernel reads the certificate files and passwords at startup and does not watch them for changes. After you update `chronicle-certificates`, restart the kernels with `kubectl -n chronicle rollout restart statefulset/chronicle`, which replaces them one at a time. Never replace the encryption certificate in place: follow [Rotating the certificate](encryption-certificate.md#rotating-the-certificate), which keeps the old one under `previous`.
- **The initial administrator.** On its first start the production image creates the administrator without a password and writes a setup token to its log. Treat the kernel's logs as sensitive until setup is done, or supply the initial password from the optional `chronicle-admin` Secret by adding this entry to the container's `env`:

  ```yaml
  - name: Cratis__Chronicle__Authentication__AdminUser__Password
    valueFrom:
      secretKeyRef:
        name: chronicle-admin
        key: password
  ```

  The kernel uses the password only while the administrator has none, and never overwrites one that is set. Once setup is done, remove the entry and the Secret, so the credential no longer sits in the environment of every kernel. Removing the entry rolls the kernels like any other change. The entry was not part of the tested manifest. See [Authentication](configuration/authentication.md#admin-user-bootstrap).

> [!CAUTION]
> Back up every encryption certificate and its password with the database backups, and keep a certificate in the backup set for longer than it stays in the ring. Deleting a certificate that an older backup still needs makes that backup permanently unreadable. Follow [Backup and restore](production.md#backup-and-restore) for the order to restore them in.

## Deploy

1. Create the namespace with `kubectl create namespace chronicle`. The manifest declares it as well, so applying the manifest later adopts it; `kubectl apply` warns once that the namespace has no last-applied annotation, and adds it.
2. Create the two Secrets with the names and keys under [Secrets](#secrets). Without them the kernel pods stay in `CreateContainerConfigError` until both exist. From files, for example:

   ```bash
   kubectl -n chronicle create secret generic chronicle-storage \
     --from-file=connection-string=./connection-string
   kubectl -n chronicle create secret generic chronicle-certificates \
     --from-file=tls.pfx=./tls.pfx --from-file=tls-password=./tls-password \
     --from-file=encryption.pfx=./encryption.pfx --from-file=encryption-password=./encryption-password
   ```

3. Apply the manifest, then the NetworkPolicies, after adapting both to your cluster.
4. Label every namespace whose applications connect to the kernel: `kubectl label namespace my-application chronicle-clients=true`. Until a namespace has the label, the policy blocks its pods.

## Verify the deployment

After deploying:

1. Wait for the rollout with `kubectl -n chronicle rollout status statefulset/chronicle`. Each kernel becomes ready once its startup has completed.
2. Check `kubectl -n chronicle get pods -o wide`: every kernel runs on its own node with no restarts.
3. Check `kubectl -n chronicle get pdb chronicle`: with every kernel ready it allows one disruption.
4. From a pod in a client namespace, request `https://chronicle.chronicle.svc.cluster.local:35000/health`. From any other namespace the request should time out.
5. Optionally, in a maintenance window, drain a node that runs a kernel with `kubectl drain <node> --ignore-daemonsets --delete-emptydir-data`. A drain is a real disruption: it evicts every pod on the node, not only the kernel. `--delete-emptydir-data` is required because of the `/tmp` volume; without it the drain refuses to evict the kernel. The other kernels keep serving, the evicted one becomes ready again where it is scheduled, and a second drain is refused while it is not ready.

## Collect crash dumps

The production image carries `createdump`, which the .NET runtime runs to write a dump when the kernel crashes. Dumps are off by default. To turn them on, give the kernel a volume for dumps and the runtime's dump settings. In the StatefulSet's container, add:

```yaml
          env:
            # ...the existing entries...
            - name: DOTNET_DbgEnableMiniDump
              value: "1"
            - name: DOTNET_DbgMiniDumpType
              value: "2" # A minidump with the managed heap, which dotnet-dump's heap commands need
            - name: DOTNET_DbgMiniDumpName
              value: /dumps/core.%h.%t # Pod name and time, so a later crash does not overwrite an earlier dump
          volumeMounts:
            # ...the existing mounts...
            - name: dumps
              mountPath: /dumps
      volumes:
        # ...the existing volumes...
        - name: dumps
          emptyDir:
            sizeLimit: 5Gi
```

Always set `DOTNET_DbgMiniDumpName`, and never point it at `/tmp`. Without it the runtime writes `/tmp/coredump.<pid>`. With `readOnlyRootFilesystem: true` only the mounted volumes are writable, and the `/tmp` volume's 256Mi `sizeLimit` is far below the size of a dump: a heap dump is about as large as the kernel's memory, up to the 2Gi limit, and a kernel that had only just started wrote 770 MiB. The kubelet evicts a pod whose `emptyDir` grows past its `sizeLimit`, so size the dumps volume for more than one dump of the memory limit and check that the nodes have that much ephemeral storage free. Setting `resources.requests.ephemeral-storage` on the container makes the scheduler account for it.

After a crash the kubelet restarts the kernel's container in the same pod, and the `emptyDir` with the dump survives that restart. It does not survive the pod being deleted, evicted or rescheduled, so copy the dump out before you delete the pod, drain its node or roll out a change.

A kernel in a crash loop writes a dump on every crash, because `%t` gives each one its own name. With heap dumps of up to 2Gi, the third one pushes the volume past its 5Gi `sizeLimit`. The kubelet then evicts the pod, and eviction deletes the `emptyDir` together with every dump on it. To keep the dumps you need:

- copy each dump out promptly and delete it from the volume once it is copied;
- use a PersistentVolumeClaim, for example from the StatefulSet's `volumeClaimTemplates`, instead of an `emptyDir` when dumps must survive the pod being evicted or rescheduled. A generic ephemeral volume keeps dumps off the node's ephemeral storage, so they no longer cause the eviction, but it is deleted with the pod;
- turn dumps off again once you have captured one.

### Copy a dump out

`kubectl cp` needs `tar` in the container it copies from, and the chiseled production image has none. Copy through an ephemeral debug container that mounts the dumps volume instead. It reads the volume directly, so it works while the kernel is down or in `CrashLoopBackOff`. This needs kubectl 1.31 or later for `--custom`. Save the partial container spec that adds the mount:

```json
{
  "volumeMounts": [
    { "name": "dumps", "mountPath": "/dumps" }
  ]
}
```

Then start the debug container with it and copy the dump:

```bash
kubectl -n chronicle debug chronicle-0 --image=busybox:1.37 --container=dumps --profile=restricted --custom=dumps-mount.json -- sleep 3600
kubectl -n chronicle exec chronicle-0 -c dumps -- ls -l /dumps
kubectl -n chronicle cp -c dumps --retries=5 chronicle-0:/dumps/core.chronicle-0.1794374298 ./core.chronicle-0.1794374298
kubectl -n chronicle exec chronicle-0 -c dumps -- rm /dumps/core.chronicle-0.1794374298
```

Replace the file name with the one `ls` lists. The pod's `runAsUser: 1654` and `runAsGroup: 1654` apply to the debug container, so it reads and deletes the kernel's dumps as the same user. The debug container stops when `sleep` ends; Kubernetes keeps its record in the pod until the pod is replaced, so give a later debug container another `--container` name. Open the dump with `dotnet-dump analyze`.

With an older kubectl, share the kernel's process namespace and read the volume through `/proc/1/root`, where PID 1 is the kernel. This needs kubectl 1.27 or later for `--profile=restricted`, and it works only while the kernel container is running: if the kernel has crashed or restarts mid-copy, `/proc/1` is gone and the copy fails or is cut short. The debug container must run as the kernel's user, which the pod-level `runAsUser` provides; with a different user it gets `Permission denied`.

```bash
kubectl -n chronicle debug chronicle-0 --image=busybox:1.37 --target=chronicle --container=dumps --profile=restricted -- sleep 3600
kubectl -n chronicle cp -c dumps --retries=5 chronicle-0:/proc/1/root/dumps/core.chronicle-0.1794374298 ./core.chronicle-0.1794374298
```

### Handle dumps as sensitive data

A dump is a copy of the kernel's memory. It contains the storage connection strings, certificate and signing keys, and event data, including personal data that compliance encryption protects at rest. Treat every dump as a secret that holds personal data: restrict who can run `kubectl debug`, `exec` and `cp` in the namespace, keep dumps encrypted and access-controlled while they are stored and moved, never attach one to a public issue, and delete every copy, locally and on the dumps volume, when the analysis is done. Erasing a person's data does not remove it from a dump that was taken earlier, so dumps fall under your data-handling and retention process.

## See also

- [Production hosting](production.md) for the storage, certificate and backup requirements.
- [Rolling deploy](rolling-deploy.md) for upgrading a running cluster.
- [Container resources](container-resources.md) for memory, garbage collection and CPU sizing.
- [Clustering](configuration/clustering.md) for clustering types, the single-writer lease and network boundaries.
- [Health Endpoint](configuration/health-endpoint.md) for the dedicated health port and readiness checks.
