---
description: What erasing an encryption key guarantees on each key store - MongoDB, HashiCorp Vault and Azure Key Vault - and what stays outside Chronicle's control.
---
# What key erasure guarantees on each key store

A data protection officer asks one question about a right-to-erasure request: once the erasure call has returned, can anything still decrypt the person's data? The answer is not the same on every key store. Chronicle deletes the key the same way everywhere, but what "deleted" means after that depends on the store holding the key, and on how you deployed it.

This page states the guarantee for each store, what Chronicle cannot promise, and what you configure to keep the guarantee. It does not claim that erasure is instant or complete on every backend. For the mechanism itself, read [The encryption key lifecycle](key-lifecycle.mdx); to choose and configure a store, read [Compliance Storage](../hosting/configuration/compliance-storage.md).

## What Chronicle does the same everywhere

An erasure records the [erasure fence](key-lifecycle.mdx#the-erasure-fence) first and then deletes every revision of the subject's key. The key store is wrapped in an in-process key cache; the cache is invalidated before and after the delete, and the erasure evicts the key from every silo's cache. After the erasure returns, Chronicle itself no longer serves the key, and it refuses to provision or accept a key for that subject until you authorize a new one.

Those guarantees are about Chronicle. They do not reach:

- Values that were never encrypted, or that were already delivered as plaintext. Key deletion cannot change what was read before it.
- Backups, replicas and snapshots of the key store that you or your platform made. See [what the fence cannot protect against](key-lifecycle.mdx#not-stopped) and [backup and restore ordering](../hosting/configuration/compliance-storage.md#backup-and-restore-ordering).

## At a glance

| Question | MongoDB (default) | HashiCorp Vault (KV v2) | Azure Key Vault |
| --- | --- | --- | --- |
| What the erase does | Deletes the key documents | Deletes the key metadata and all versions of every revision path | Soft-deletes each revision secret, then purges it |
| Is the material recoverable afterwards through the store? | No | No | Not once the purge succeeds. If the purge is refused, yes, until the retention period ends |
| What fails the erasure loudly | Any driver error | Any Vault error | A refused purge (HTTP 403) throws `EncryptionKeyNotPurged` |
| Where a read can still return the key | Primary reads during a failover window | A stale read on a non-strongly-consistent deployment | A failed-over vault, if the delete had not replicated |
| What you must configure | A replica set, backups with the same retention as your erasure promise | Strong consistency for the storage and read routing for your topology | Purge permission for Chronicle's identity, and a vault without purge protection |

The sections below give the detail behind each column.

## MongoDB

Source: `Source/Kernel/Storage.MongoDB/Encryption/EncryptionKeyStorage.cs`.

The key store is the default. Erasing a subject's key calls `DeleteMany` on the key documents for the identifier in the `(event store, namespace)` key collection. The fence is a separate document in an `encryption-key-erasures` collection for the same event store and namespace.

- **What the delete guarantees.** The documents are removed from the database. There is no soft delete, retention window or recycle bin.
- **When a read is guaranteed to see it.** Chronicle sets no read or write concern of its own for the key store, so the options on your MongoDB connection string and the server defaults apply. During a replica-set failover, reads and writes on the primary are accepted for a window that Chronicle does not bound. An erasure that coincides with a failover is one to confirm and, if in doubt, repeat. This behavior is tracked as [Chronicle issue 4977](https://github.com/Cratis/Chronicle/issues/4977).
- **What stays outside.** Backups, `mongodump` files, oplog or snapshot copies and replicas hold the deleted documents until they expire. Your backup retention is your erasure latency.
- **Not tested or claimed.** Sharded MongoDB.

## HashiCorp Vault

Source: `Source/Kernel/Storage.Vault/VaultEncryptionKeyStorage.cs`.

Chronicle stores each revision at its own path in a KV v2 mount. To erase, it lists the revisions and calls the KV v2 [delete metadata and all versions](https://developer.hashicorp.com/vault/api-docs/secret/kv/kv-v2) endpoint for each path. That endpoint permanently removes the key metadata and all version data, so Vault's own soft-delete and undelete do not apply: Chronicle never uses the version-level `delete`, which is recoverable.

- **What the delete guarantees.** The secret versions are gone from Vault's storage backend once the call is acknowledged.
- **When a read is guaranteed to see it.** That depends on how you run Vault, not on Chronicle:
  - Without Enterprise performance standbys, Vault [documents](https://developer.hashicorp.com/vault/docs/enterprise/consistency) the same behavior as the default Consul consistency model. [Consul documents](https://developer.hashicorp.com/consul/api-docs/features/consistency) that a follower, or a leader during an election, can answer with stale values. A read straight after an erasure can therefore still return the key on a Consul-backed Vault.
  - With a Consul storage backend, [`consistency_mode`](https://developer.hashicorp.com/vault/docs/configuration/storage/consul) accepts `default` (the default) or `strong`. Chronicle does not set or check it. Whether `strong` closes the window for Chronicle erasure end to end has not been tested.
  - With Enterprise performance standbys, the request header `X-Vault-Forward: active-node` sends a read to the active node. Forwarding by header is disabled unless `allow_forwarding_via_header` is enabled, and it does not help with performance replication. Chronicle does not send this header, so a deployment that needs it has to route Chronicle's reads through a proxy or Vault agent that adds it.
  - With integrated storage and no performance standbys, Vault's own documentation applies; Chronicle adds nothing to it.
- **What stays outside.** Vault snapshots, Consul snapshots and replicated clusters.

## Azure Key Vault

Source: `Source/Kernel/Storage.AzureKeyVault/AzureKeyVaultEncryptionKeyStorage.cs`, `EncryptionKeyNotPurged.cs`.

Chronicle stores each revision as a secret named `chronicle--{event-store}--{namespace}--{identifier}--{revision}`. Erasing a revision does two things:

1. It soft-deletes the secret and waits for the delete to finish.
2. It purges the deleted secret.

Soft-delete alone is not an erasure. Azure Key Vault [keeps a soft-deleted secret recoverable](https://learn.microsoft.com/en-us/azure/key-vault/general/soft-delete-overview) for a retention period of 7 to 90 days (90 by default), and Chronicle treats a key that is only soft-deleted as not erased.

### What you must configure

- **Purge permission.** The identity Chronicle runs as needs permission to purge deleted secrets in addition to the Get, List, Set and Delete permissions listed in [Compliance Storage](../hosting/configuration/compliance-storage.md#azure-key-vault). Chronicle also lists deleted secrets to finish an earlier incomplete erasure, which needs permission to list them.
- **No purge protection.** With [purge protection](https://learn.microsoft.com/en-us/azure/key-vault/general/soft-delete-overview) enabled, a deleted object cannot be purged until the retention period passes. Such a vault cannot support Chronicle's erasure. Chronicle does not detect the setting in advance; the first erasure fails.

### What happens when the purge is refused

The vault answers the purge with HTTP 403. The erasure fails with `EncryptionKeyNotPurged`, which names the secret and the status. The state at that point is:

- The secret is soft-deleted, so it is no longer returned by an ordinary read, but anyone allowed to recover secrets can recover it until the retention period ends.
- The erasure is not complete, and Chronicle does not report it as complete.
- Repeating the erasure after you grant the permission completes it. Revisions that the failed attempt soft-deleted are found again through the deleted-secrets list.

Recoverability of the material does not by itself make it readable through Chronicle, because the fence rejects provisioning and saves for an erased subject. Treat that as a second line of defense rather than the erasure: a recovered secret is still a key that Azure was asked to destroy. The read-side validation that would make this airtight on every path is tracked as [Chronicle issue 4978](https://github.com/Cratis/Chronicle/issues/4978); until it is in the version you run, do not rely on the fence alone.

### Failover and replication

Key Vault replicates within the region and, [asynchronously](https://learn.microsoft.com/en-us/azure/reliability/reliability-key-vault), to the paired region. After a Microsoft-managed failover, the vault is read-only and can serve the secrets that were replicated before it. A delete or an erasure fence written shortly before the failover can be missing in the secondary, and a read-only vault cannot take the repeat. Repeat the erasure once the primary is back, and verify the fence.

## Writing the guarantee into your procedure

Chronicle gives you the following to build on:

- A completed erasure is logged at information level; a partial one is logged at error level and throws. See [What Chronicle records](key-lifecycle.mdx#what-chronicle-records-and-what-is-still-yours).
- A store that cannot erase fails the call. It does not report success.

You decide, and document for your own audit:

- Your store's consistency configuration, from the table above.
- Your backup retention for the key store, which bounds how long a deleted key can be restored from a backup. Restoring such a backup removes the fence with it.
- Whether the retention period of an Azure vault, or the lifetime of a MongoDB or Vault backup, matches what you tell data subjects.

## See also

| Topic | Description |
|---|---|
| [The encryption key lifecycle](key-lifecycle.mdx) | The fence, and what it refuses |
| [Erasing a subject](erasing-a-subject) | The call to make, and how to certify an erasure is complete |
| [Compliance Storage](../hosting/configuration/compliance-storage.md) | Configure each key store |
