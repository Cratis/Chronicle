# Compliance Storage

Chronicle stores encryption keys alongside the rest of your application data by default. When you configure a dedicated compliance storage, those keys are stored in a separate, independently secured backend — such as HashiCorp Vault — so that key material never resides in the same database as the encrypted events.

If no compliance storage is explicitly configured, Chronicle uses the general [storage](storage.md) backend for encryption keys.

## Configuration

The `compliance` section mirrors the structure of the top-level `storage` section. Set a `type` and `connectionDetails` inside `compliance.storage`:

```json
{
  "compliance": {
    "storage": {
      "type": "<storage-type>",
      "connectionDetails": "<connection-string-or-url>"
    }
  }
}
```

## Vault

HashiCorp Vault provides a purpose-built secrets backend that is well-suited for storing PII encryption keys. Chronicle uses the [KV v2 secrets engine](https://developer.hashicorp.com/vault/docs/secrets/kv/kv-v2) to store each key revision at a distinct path.

### Authentication

Chronicle authenticates to Vault using a [token](https://developer.hashicorp.com/vault/docs/auth/token). The token is read from the `VAULT_TOKEN` environment variable at startup. Ensure this variable is set before the Chronicle server process starts.

### Configuration

```json
{
  "compliance": {
    "storage": {
      "type": "vault",
      "connectionDetails": "http://vault:8200"
    }
  }
}
```

Set `VAULT_TOKEN` in the environment:

```shell
export VAULT_TOKEN=s.myVaultToken
```

> **Note:** Never include the Vault token in the `connectionDetails` string or in `chronicle.json`. Always pass it through the environment to avoid storing secrets in your configuration files.

### KV v2 mount point

Chronicle uses the `secret` KV v2 mount point by default. Encryption keys are organized under a path derived from the event store name, namespace, and subject identifier.

### Key paths

Encryption keys are stored at:

```text
secret/<event-store>/<namespace>/<identifier>/<revision>
```

Each revision is an independent secret, which means individual revisions can be deleted without affecting others (for example, when rotating keys or when the full key history is required for a limited time).

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| type | string | Yes | Must be `vault` |
| connectionDetails | string | Yes | The Vault server address, for example `http://vault:8200` |
