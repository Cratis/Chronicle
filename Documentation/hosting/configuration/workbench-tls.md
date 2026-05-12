# Workbench TLS Configuration

Chronicle supports a dedicated TLS configuration for the Workbench (admin UI), separate from the gRPC TLS configuration. This allows deployments behind an ingress or reverse proxy to disable Workbench TLS while configuring gRPC TLS independently.

## Fallback behavior

The Workbench TLS configuration follows this resolution chain:

1. If `workbench.tls` is set, use it (including `enabled: false` to disable TLS)
2. If `workbench` is set but `workbench.tls` is not, fall back to the top-level `tls`
3. If `workbench` is not set, fall back to the top-level `tls`

This means existing configurations without `workbench` continue to work as before — no breaking change.

## Configuration

### Disable Workbench TLS (ingress/reverse proxy deployment)

When deploying behind a reverse proxy that terminates TLS:

```json
{
  "tls": {
    "enabled": true,
    "certificatePath": "/certs/server.pfx",
    "certificatePassword": "your-password"
  },
  "workbench": {
    "tls": {
      "enabled": false
    }
  }
}
```

In this configuration:
- gRPC uses TLS with the provided certificate
- The Workbench runs without TLS, relying on the upstream proxy for HTTPS

### Separate Workbench certificate

To use a different certificate for the Workbench:

```json
{
  "tls": {
    "certificatePath": "/certs/grpc.pfx",
    "certificatePassword": "grpc-password"
  },
  "workbench": {
    "tls": {
      "certificatePath": "/certs/workbench.pfx",
      "certificatePassword": "workbench-password"
    }
  }
}
```

### Environment variables

```bash
# Top-level TLS (used by gRPC, and Workbench fallback)
Cratis__Chronicle__Tls__CertificatePath=/certs/server.pfx
Cratis__Chronicle__Tls__CertificatePassword=your-password

# Workbench-specific TLS
Cratis__Chronicle__Workbench__Tls__Enabled=false
```

## Deployment examples

### Azure Container Apps

ACA terminates TLS at the ingress level. Chronicle's Workbench does not need to handle TLS:

```json
{
  "tls": {
    "certificatePath": "/certs/grpc.pfx",
    "certificatePassword": "your-password"
  },
  "workbench": {
    "tls": {
      "enabled": false
    }
  }
}
```

### Kubernetes with Nginx Ingress

Nginx ingress handles TLS termination for HTTP traffic. gRPC traffic is passed through directly:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: chronicle-config
data:
  chronicle.json: |
    {
      "tls": {
        "certificatePath": "/certs/grpc.pfx",
        "certificatePassword": "from-secret"
      },
      "workbench": {
        "tls": {
          "enabled": false
        }
      }
    }
```

Mount the gRPC certificate from a Kubernetes Secret:

```yaml
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
        - name: chronicle
          volumeMounts:
            - name: config
              mountPath: /app/chronicle.json
              subPath: chronicle.json
            - name: tls-certs
              mountPath: /certs
              readOnly: true
      volumes:
        - name: config
          configMap:
            name: chronicle-config
        - name: tls-certs
          secret:
            secretName: chronicle-grpc-tls
```

### Direct TLS (no proxy)

When Chronicle handles all TLS directly (no reverse proxy):

```json
{
  "tls": {
    "certificatePath": "/certs/server.pfx",
    "certificatePassword": "your-password"
  }
}
```

Both gRPC and Workbench will use the same certificate. No `workbench` section needed.
