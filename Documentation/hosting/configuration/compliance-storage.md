# Compliance Storage

Chronicle stores encryption keys alongside the rest of your application data by default. When you configure a dedicated compliance storage, those keys are stored in a separate, independently secured backend — such as HashiCorp Vault — so that key material never resides in the same database as the encrypted events.

If no compliance storage is explicitly configured, Chronicle uses the general [storage](storage) backend for encryption keys.

## Configuration

The `compliance` section now contains an `encryption` subsection that holds the `storage` configuration. Set a `type` and `connectionDetails` inside `compliance.encryption.storage`:

```json
{
  "compliance": {
    "encryption": {
      "storage": {
        "type": "<storage-type>",
        "connectionDetails": "<connection-string-or-url>"
      }
    }
  }
}
```

## Migrating from the default storage

If you are configuring a dedicated compliance storage on a system that has already been running, the keys you care about are in the general storage backend — and the new store is empty. Turning it on by itself is a one-way flip: those keys become unreachable, and every `[PII]` value they protect reads back as an **empty string**. Nothing reports it. No exception is thrown, nothing is logged, and the health endpoint stays green — the result is byte-for-byte what a completed [right-to-erasure](../../compliance/index) looks like.

Set `migrateFromDefaultStorage` and there is no flip at all:

```json
{
  "compliance": {
    "encryption": {
      "storage": {
        "type": "vault",
        "connectionDetails": "http://vault:8200"
      },
      "migrateFromDefaultStorage": true
    }
  }
}
```

Both stores are now live, and Chronicle keeps them in step for you:

- A key is looked for in the dedicated store first. When it is only in the default storage, it is served from there and **written into the dedicated store as it is read** — so the migration happens through ordinary traffic, with no script to write and no verify pass to run.
- New keys are provisioned in the dedicated store and mirrored back to the default storage, so both stay complete. That is what makes the move reversible: set `migrateFromDefaultStorage` back to `false` — or drop the `storage` section entirely — and nothing is lost.
- Erasing a key erases it from **both** stores. A deletion that only reaches one of them fails loudly rather than reporting success, because a key surviving in either store is not an erasure.

A key moves the first time it is *read*, so a subject whose data nobody has queried still lives only in the default storage. Before you turn `migrateFromDefaultStorage` off, confirm the dedicated store actually holds every key — turning it off early puts the subjects that were never read straight back into the empty-string outcome above. Once you have confirmed it, removing the leftover keys from the general storage backend is an ordinary cleanup you decide on separately, not the irreversible last step of a sequence.

> **Note:** Leaving `migrateFromDefaultStorage` on indefinitely is valid but rarely what you want — key material keeps being written to the same database as the encrypted data, which is the separation a dedicated compliance storage exists to give you.

The setting has no effect unless `storage` is configured. Without a dedicated store there is nothing to migrate to, and the general storage backend serves the keys on its own.

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| migrateFromDefaultStorage | bool | No | Keep the general storage backend serving encryption keys alongside `storage`, moving each key into `storage` as it is read. Defaults to `false` |

As an environment variable:

```shell
export Cratis__Chronicle__Compliance__Encryption__MigrateFromDefaultStorage=true
```

## Vault

HashiCorp Vault provides a purpose-built secrets backend that is well-suited for storing PII encryption keys. Chronicle uses the [KV v2 secrets engine](https://developer.hashicorp.com/vault/docs/secrets/kv/kv-v2) to store each key revision at a distinct path.

### Authentication

Chronicle authenticates to Vault using a [token](https://developer.hashicorp.com/vault/docs/auth/token). The token is read from the `VAULT_TOKEN` environment variable at startup. Ensure this variable is set before the Chronicle server process starts.

### Configuration

```json
{
  "compliance": {
    "encryption": {
      "storage": {
        "type": "vault",
        "connectionDetails": "http://vault:8200"
      }
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

## Azure Key Vault

Azure Key Vault provides a fully managed, cloud-native secrets backend for storing PII encryption keys. Chronicle uses the Azure Key Vault Secrets API to store each key revision as a distinct secret.

### Authentication

Chronicle authenticates to Azure Key Vault using [`DefaultAzureCredential`](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential). This supports multiple authentication methods in order:

- Environment variables (`AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`, `AZURE_TENANT_ID`)
- Workload identity
- Managed identity
- Azure CLI credentials
- Visual Studio / VS Code credentials

Ensure that the identity used has the **Key Vault Secrets Officer** role (or at minimum **Get**, **List**, **Set**, and **Delete** secret permissions) on the target Key Vault.

### Configuration

```json
{
  "compliance": {
    "encryption": {
      "storage": {
        "type": "azure-key-vault",
        "connectionDetails": "https://my-vault.vault.azure.net"
      }
    }
  }
}
```

### Secret naming

Encryption keys are stored as individual secrets. Secret names follow this pattern:

```text
chronicle--{event-store}--{namespace}--{identifier}--{revision}
```

Each component is sanitized to lowercase alphanumeric characters with single hyphens replacing any other character sequences. Double hyphens (`--`) serve as unambiguous separators between components.

| Property | Type | Required | Description |
| --- | --- | --- | --- |
| type | string | Yes | Must be `azure-key-vault` |
| connectionDetails | string | Yes | The Azure Key Vault URI, for example `https://my-vault.vault.azure.net` |
