---
title: Run Chronicle on Kubernetes
description: Deploy the Cratis Chronicle production image to Kubernetes with a tested reference manifest, probes, a PodDisruptionBudget, a NetworkPolicy and secret handling.
---

Run the Chronicle Kernel on Kubernetes from the manifest on this page: a StatefulSet with probes matched to how the kernel starts, a PodDisruptionBudget, spread across nodes, a locked-down security context and a NetworkPolicy. The manifest was applied to a three-node cluster with the `19.153.2` production image: both kernels became ready, and a node drain evicted one kernel, kept the other serving and rescheduled the evicted one once a node was free.

Read [Production hosting](production.md) first. This page assumes the storage, TLS and encryption-certificate requirements described there, and adds what is specific to Kubernetes.

## Choose a StatefulSet

Use a StatefulSet, even though the kernel keeps all of its state in the storage backend and needs no persistent volume:

- **Stop before start.** A StatefulSet never runs two pods with the same name. On a rolling update or an eviction it stops the old pod before it creates the replacement. A single kernel with the default `Localhost` clustering needs exactly that: the replacement waits for the [single-writer lease](configuration/clustering.md#single-writer-lease), which the old kernel releases when it stops.
- **One node at a time.** A rolling update replaces one pod and waits for it to become ready before it moves to the next, which is how [Rolling deploy](rolling-deploy.md) asks you to replace nodes.
- **Stable names.** Pod names such as `chronicle-0` appear as the host name in the cluster membership table and in logs, so a restarted kernel is easy to recognize.

A Deployment also works. With more than one kernel, use `MongoDB` or `Sql` clustering and the default rolling update. With a single `Localhost` kernel, set `strategy: Recreate`, or `RollingUpdate` with `maxSurge: 0` and `maxUnavailable: 1`. The default one-replica Deployment rollout (`maxSurge: 1`) never converges: the new pod waits for the lease while Kubernetes waits for it to become ready.

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

Every kernel has the same `ClusterId`, `ServiceId` and clustering type, which has to match the storage: `MongoDB` here, `Sql` for PostgreSQL or SQL Server. `AdvertisedIP` takes the pod IP from the downward API, so each silo binds and advertises the address its peers can route to.

The manifest turns the Orleans gateway (port 30000) off, because applications that use the Chronicle clients talk to port 35000. Keep it on only for Orleans clients, and add the [gateway rule](#allow-the-orleans-gateway) to the NetworkPolicy. The membership table then shows a gateway port of 0 for each silo.

| Port | Purpose | Reached by |
| --- | --- | --- |
| 35000 | gRPC, REST API, Workbench and OAuth over TLS | Applications, your ingress |
| 11111 | Orleans silo-to-silo protocol | Other kernels only |
| 30000 | Orleans gateway, off in this manifest | Trusted Orleans clients only |
| 8080 | Dedicated health port, plaintext HTTP/1.1 | The kubelet's probes |

The image also sets `ASPNETCORE_HTTP_PORTS=8080`. The kernel configures its listeners itself and logs that it overrides that address, so the variable does not open a second listener. Port 8080 here is the health port from `Health__Port`.

`Health__Exclusive=true` makes port 8080 answer only `/health`, `/health/live` and `/health/ready`, and 404 for everything else. Without it the plaintext health port is a second, unencrypted way into the Workbench and the API. Route your ingress for the Workbench to port 35000, as [Workbench configuration](configuration/workbench.md#with-an-exclusive-health-port) shows.

### Probes

The kernel binds its listeners before it starts the Orleans silo, so each probe checks a different thing:

| Probe | Target | What it checks |
| --- | --- | --- |
| Startup | TCP on 35000 | The process is up and has bound its listeners |
| Liveness | `GET /health/live` on 8080 | The process answers HTTP. It runs no health checks and stays successful while the silo starts |
| Readiness | `GET /health` on 8080 | The silo and Chronicle's startup work have completed, the storage checks pass and shutdown has not begun |

Readiness keeps a starting kernel out of the `chronicle` Service without restarting it. Never use the readiness endpoint, or a TCP check, as the liveness probe: a kernel that is still catching up would be killed and start over. `/health/ready` answers the same as `/health`. [Health Endpoint](configuration/health-endpoint.md) describes every check that contributes to readiness.

### Stopping

When Kubernetes stops a pod it removes it from the Service endpoints and sends `SIGTERM` at the same time. The `preStop` sleep keeps the kernel serving for five seconds while load balancers and kube-proxy catch up. The `sleep` action runs inside the kubelet, so it works in the production image, which has no shell and no `sleep` binary.

The kernel then has 10 seconds to stop in an orderly way: its host shutdown timeout is fixed at 10 seconds, and the shutdown drain inside it defaults to 8 seconds. `terminationGracePeriodSeconds` covers the `preStop` sleep and that shutdown, so 30 seconds leaves room for both. Keep it at 30 or more. In the test a drained kernel left the cluster in an orderly way within six seconds of the eviction, and its membership row turned `Dead` rather than waiting to be suspected. See [Graceful shutdown](configuration/root-properties.md#graceful-shutdown).

### Disruptions and placement

The PodDisruptionBudget lets a drain, a cluster upgrade or the autoscaler evict one kernel at a time. In the test, draining the node of `chronicle-0` succeeded at once; draining the second node while `chronicle-0` was still unavailable was refused with `Cannot evict pod as it would violate the pod's disruption budget`, so the last kernel kept serving.

Required pod anti-affinity keeps two kernels off the same node, so a node failure takes down at most one. It also means an evicted kernel needs a free node: with two kernels on two worker nodes, the evicted kernel stays `Pending` until its node comes back. Have at least one node more than kernels if the cluster has to stay at full size during maintenance, or use `preferredDuringSchedulingIgnoredDuringExecution` to accept two kernels on one node for a while. The topology spread constraint additionally spreads the kernels over zones where the cluster has them, without blocking scheduling when it does not.

With a single kernel, keep `maxUnavailable: 1`. Eviction then costs a short outage while the kernel restarts elsewhere; in the test it was ready on the other node 11 seconds after the drain finished. Do not use `minAvailable: 1` with one replica: no drain could ever evict the pod.

### Security context

The production image runs as the non-root user `1654` from `/app`, on a chiseled base image without a shell. The manifest makes that explicit and locks the rest down:

- `runAsNonRoot`, `runAsUser: 1654` and `runAsGroup: 1654` match the image's user. `fsGroup: 1654` lets that group read the Secret files, which are mounted with mode `0440`.
- `readOnlyRootFilesystem: true`. The kernel writes nothing under `/app`; the Data Protection key ring is stored in the storage backend, not on disk, so it needs no volume.
- An `emptyDir` on `/tmp`. The .NET runtime creates its diagnostics socket and debugger pipes there; `dotnet-counters`, `dotnet-trace` and `dotnet-dump` need them to attach to the kernel.
- No privilege escalation, every capability dropped, the runtime's default seccomp profile, and no service-account token, because the kernel does not call the Kubernetes API.

### Resources

Requests and limits depend on your workload. Memory follows the number of resident observers, reactors and reducers, and CPU peaks during a cold start. Set them from your own measurements as described in [Container resources](container-resources.md). The manifest sets no CPU limit, so a kernel that is starting can use idle CPU on its node.

## Budgets for a cold start

A kernel that starts against a large backlog is busy for a while: it reactivates observers, catches up and fills its caches. Because the kernel binds its listeners first, the startup probe passes within seconds, even before that work starts. The 300-second startup budget (`failureThreshold: 30` × `periodSeconds: 10`) only covers a process that is slow to start at all.

During the cold start, readiness keeps the kernel out of the Service for as long as it needs; it never restarts the pod. Liveness is what can restart it. `/health/live` runs no checks, but on a CPU-starved kernel even that request can wait in the thread pool. The manifest allows six failures 10 seconds apart, each with a 5-second timeout, so about a minute without an answer, before the kubelet restarts the kernel. Do not tighten that. A restart throws away the work done so far and starts the next cold start from the beginning.

Two things a probe budget cannot fix:

- **Starved CPU.** A CPU limit below the cold-start peak starves the thread pool, and a kernel that cannot complete a startup step stops itself and is restarted. Size CPU from the peak, as [Container resources](container-resources.md#cpu-is-a-cold-start-concern) describes.
- **Startup failures.** A startup step that exhausts its retries stops the kernel. Raising `failureThreshold` does not change that. [Know what a starting node does](rolling-deploy.md#know-what-a-starting-node-does) explains the startup steps and their retries, and [Handle a node that does not come up](rolling-deploy.md#6-handle-a-node-that-does-not-come-up) how to read their failures.

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

Some deployments terminate TLS at an ingress and run the kernel with `Cratis__Chronicle__Tls__Enabled=false`, often together with `Cratis__Chronicle__Authentication__Enabled=false`. The manifest then works without the TLS certificate, and the probes are unchanged, because the health port is plaintext HTTP/1.1 either way. The main port changes:

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
| One kernel, `Localhost` clustering | StatefulSet with one replica and `RollingUpdate`: it stops the old pod first. For a Deployment, use `Recreate`. Never use a Deployment's default rolling update |
| Several kernels, `MongoDB` or `Sql` clustering, both releases can share a cluster | Rolling update, one kernel at a time, waiting for readiness |
| An upgrade the [compatibility table](rolling-deploy.md#check-that-the-two-versions-can-share-a-cluster) or the [supported source releases](rolling-deploy.md#supported-source-releases) mark as stop-all, such as a new major or a release with a compatibility exception | Stop every kernel, then start the new release: `kubectl scale statefulset chronicle --replicas=0`, wait until the pods are gone, then scale back up. For a Deployment, use `Recreate` for that upgrade |
| A configuration change on the same release | The same strategy as for an upgrade between compatible releases |

Stopping every kernel first is always safe. What fails across a compatibility exception is the overlap of an old and a new kernel in one cluster, and a rolling update creates exactly that overlap. A new kernel that finds an incompatible release in the cluster logs a critical message and stops, and the rollout stalls with the old kernels still running.

## Secrets

The manifest reads two Secrets. Create them from your secret store, for example through a secrets operator or the CSI secrets-store driver; the names and keys below are what the manifest expects.

| Secret | Key | Used for |
| --- | --- | --- |
| `chronicle-storage` | `connection-string` | The storage connection string, read from a file through `Storage__ConnectionDetailsFile` |
| `chronicle-certificates` | `tls.pfx` | The TLS certificate for port 35000 |
| `chronicle-certificates` | `tls-password` | Its password, passed as `Tls__CertificatePassword` |
| `chronicle-certificates` | `encryption.pfx` | The encryption certificate |
| `chronicle-certificates` | `encryption-password` | Its password, passed as `EncryptionCertificate__CertificatePassword` |

- **Storage connection.** Mount the connection string as a file rather than an environment variable, so it does not show up in the pod's environment. The kernel reads the file once at startup and trims a trailing newline. Setting both `ConnectionDetails` and `ConnectionDetailsFile` stops startup. See [Read the connection from a file](configuration/storage.mdx#read-the-connection-from-a-file).
- **TLS certificate.** Include every name clients use in the certificate's subject alternative names, such as `chronicle.chronicle.svc.cluster.local`, and make clients validate it with `skipTlsValidation=false`. See [Client TLS](../configuration/tls.mdx) and [Server connection strings](../connection-strings/server.md).
- **Encryption certificate.** It protects the Data Protection keys and webhook credentials, and the internal OAuth authority signs and encrypts its tokens with it, so it has to be an RSA certificate with its private key. If it has a key-usage extension, include digital signature as well as key and data encipherment; without digital signature the kernel stops at startup with `The specified certificate is not a signing certificate.` Give every kernel the same certificate and the same `previous` list. See [Data Protection key encryption](encryption-certificate.md).
- **Data Protection keys.** They live in the storage backend, encrypted with the encryption certificate, so no kernel needs a key volume. A database backup carries the key ring; it does not carry the certificate that unlocks it.
- **Certificate passwords.** The kernel reads them from configuration, so they arrive through `secretKeyRef` environment variables. The pod specification holds only the reference, but anyone who can run commands in the pod or read the Secret can read the value, so limit both.
- **The initial administrator.** On its first start the production image creates the administrator without a password and writes a setup token to its log. Treat the kernel's logs as sensitive until setup is done, or supply the initial password from a Secret through `Cratis__Chronicle__Authentication__AdminUser__Password`. See [Authentication](configuration/authentication.md#admin-user-bootstrap).

> [!CAUTION]
> Back up every encryption certificate and its password with the database backups, and keep a certificate in the backup set for longer than it stays in the ring. Deleting a certificate that an older backup still needs makes that backup permanently unreadable. Follow [Backup and restore](production.md#backup-and-restore) for the order to restore them in.

## Verify the deployment

After applying the manifest:

1. Wait for the rollout with `kubectl -n chronicle rollout status statefulset/chronicle`. Each kernel becomes ready once its startup has completed.
2. Check `kubectl -n chronicle get pods -o wide`: every kernel runs on its own node with no restarts.
3. Check `kubectl -n chronicle get pdb chronicle`: with every kernel ready it allows one disruption.
4. From a pod in a client namespace, request `https://chronicle.chronicle.svc.cluster.local:35000/health`. From any other namespace the request should time out.
5. Drain a node that runs a kernel with `kubectl drain <node> --ignore-daemonsets`. The other kernels keep serving, the evicted one becomes ready again where it is scheduled, and a second drain is refused while it is not ready.

## See also

- [Production hosting](production.md) for the storage, certificate and backup requirements.
- [Rolling deploy](rolling-deploy.md) for upgrading a running cluster.
- [Container resources](container-resources.md) for memory, garbage collection and CPU sizing.
- [Clustering](configuration/clustering.md) for clustering types, the single-writer lease and network boundaries.
- [Health Endpoint](configuration/health-endpoint.md) for the dedicated health port and readiness checks.
