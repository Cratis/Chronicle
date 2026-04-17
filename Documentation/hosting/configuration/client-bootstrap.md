# Client Bootstrap Configuration

Chronicle supports declarative client configuration, allowing clients (clientId/clientSecret pairs) to be defined at startup via configuration or secrets management, rather than exclusively through the Workbench UI.

## Configuration

### Configuration file

```json
{
  "clients": [
    {
      "clientId": "my-service",
      "clientSecret": "a-strong-secret-value"
    },
    {
      "clientId": "another-service",
      "clientSecret": "another-secret-value"
    }
  ]
}
```

### Environment variables

```bash
Cratis__Chronicle__Clients__0__ClientId=my-service
Cratis__Chronicle__Clients__0__ClientSecret=a-strong-secret-value
Cratis__Chronicle__Clients__1__ClientId=another-service
Cratis__Chronicle__Clients__1__ClientSecret=another-secret-value
```

## Properties

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| clientId | string | Yes | The client identifier used for authentication |
| clientSecret | string | Yes | The client secret in plaintext. Hashed internally on load |

## How it works

1. On startup, Chronicle reads the `clients` array from configuration
2. For each client, the secret is hashed using ASP.NET Core's `PasswordHasher`
3. The raw secret is **never retained** in memory beyond the bootstrap phase
4. If a client with the same `clientId` already exists, it is skipped (not overwritten)
5. Bootstrap clients are visible in the Workbench UI alongside manually created clients
6. The Workbench UI continues to support adding and revoking clients at runtime

## Secret management

The `clientSecret` is provided as plaintext in configuration. In production, use a proper secrets management solution:

### Azure Key Vault

```bash
# Store secret in Key Vault
az keyvault secret set --vault-name my-vault --name chronicle-client-secret --value "strong-secret"

# Reference in environment variable
Cratis__Chronicle__Clients__0__ClientId=my-service
Cratis__Chronicle__Clients__0__ClientSecret=@Microsoft.KeyVault(SecretUri=https://my-vault.vault.azure.net/secrets/chronicle-client-secret)
```

### Kubernetes Secrets

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: chronicle-clients
type: Opaque
stringData:
  client-secret: "strong-secret-value"
---
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
        - name: chronicle
          env:
            - name: Cratis__Chronicle__Clients__0__ClientId
              value: "my-service"
            - name: Cratis__Chronicle__Clients__0__ClientSecret
              valueFrom:
                secretKeyRef:
                  name: chronicle-clients
                  key: client-secret
```

### Docker Compose with secrets

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest
    environment:
      - Cratis__Chronicle__Clients__0__ClientId=my-service
      - Cratis__Chronicle__Clients__0__ClientSecret=${CLIENT_SECRET}
```

## Relationship with Workbench-managed clients

- Bootstrap clients and Workbench-managed clients coexist
- Bootstrap clients are registered as regular applications visible in the Workbench
- If a bootstrap client already exists (matching `clientId`), it is not re-registered or overwritten
- Clients created through the Workbench can be revoked through the Workbench; bootstrap clients will be re-created on next restart if removed
