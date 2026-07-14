# Health Endpoint

By default Chronicle serves its health check on the main port (35000) — the same port that carries gRPC, the Workbench, the REST API and OAuth. That single port always uses TLS, because Kestrel can only multiplex HTTP/2 (gRPC) and HTTP/1.1 on one port when ALPN negotiates the protocol per connection, and ALPN requires TLS. In development that TLS certificate is self-signed and generated automatically.

That is exactly where orchestrator and load-balancer probes run into trouble. A health probe that has to speak TLS — and sometimes validate the certificate — against a port that may be serving a self-signed certificate is awkward at best and broken at worst. The dedicated health port removes that friction: it publishes the health endpoint on its own HTTP/1.1 port where you can turn TLS off.

## Configuration

Set a `health` section with a `port` to expose the health endpoint on a dedicated port. When `port` is not set, nothing changes — the health endpoint stays on the main port.

```json
{
  "port": 35000,
  "healthCheckEndpoint": "/health",
  "health": {
    "port": 8080,
    "tls": false
  }
}
```

### Environment variables

```bash
Cratis__Chronicle__Health__Port=8080
Cratis__Chronicle__Health__Tls=false
```

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| health.port | number | null | Dedicated port for the health endpoint. When not set, the health endpoint is served on the main port. |
| health.tls | boolean | true | Whether the dedicated health port uses TLS. Only applies when `health.port` is set. |

The endpoint path is the shared `healthCheckEndpoint` (default `/health`), so with the configuration above the health endpoint is reachable at `http://<host>:8080/health`.

## Behavior

- **`health.port` not set** — the health endpoint is served on the main port (35000) over TLS, alongside all other traffic. This is the default.
- **`health.port` set** — the health endpoint is additionally served on that dedicated HTTP/1.1 port. gRPC is never exposed on it, because gRPC needs HTTP/2 and the dedicated port serves HTTP/1.1 only.
- **`health.tls` false** — the dedicated port serves the health endpoint in cleartext. The main port still requires TLS regardless of this setting; disabling TLS here only affects the dedicated health port.
- **`health.port` equal to the main port** — treated as not set, since a second listener cannot bind the port the main listener already owns.

The dedicated port is an additional listener on the same application rather than a separate, health-only server. The HTTP/1.1 endpoints (Workbench, REST API, OAuth) are therefore reachable on it too — so treat it as an internal, cluster-local probe port and do not expose it publicly, especially with TLS disabled.

## Why a plaintext health port helps in Kubernetes

Kubernetes probes and self-signed certificates do not always get along, and the details are easy to get wrong.

The kubelet's own HTTP probes, when configured with `scheme: HTTPS`, **skip TLS certificate verification** — so a self-signed certificate technically works for kubelet liveness, readiness and startup probes. If the kubelet were the only thing probing Chronicle, the main TLS port would be fine.

In practice it usually is not the only thing. Many deployments front the pod with a cloud load balancer, ingress controller, service mesh or managed health check, and those probers commonly **do** validate the certificate — and reject a self-signed one. Probes also cannot present a client certificate, so mutual TLS is not an option for them. The result is that "the pod is healthy but the probe fails" class of problem, caused entirely by certificate validation rather than by Chronicle's actual health.

Publishing the health endpoint on a dedicated plaintext port sidesteps certificate validation for every kind of prober:

```json
{
  "health": {
    "port": 8080,
    "tls": false
  }
}
```

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 8080
    scheme: HTTP
readinessProbe:
  httpGet:
    path: /health
    port: 8080
    scheme: HTTP
```

gRPC clients keep using the TLS main port — gRPC is never served on the plaintext port — while the probes target the dedicated port.

## Related pages

- [TLS Configuration (Server)](tls.md) for configuring the main port's certificate.
- [Port Reference](port-reference.md) for the full list of ports Chronicle exposes.
- [Root Properties](root-properties.md) for the `healthCheckEndpoint` path.
