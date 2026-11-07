---
title: Key-store erasure guarantees in Cratis Chronicle
description: Cratis Chronicle deletes subject keys, but recovery and consistency depend on the key store. Compare MongoDB, HashiCorp Vault and Azure Key Vault.
---

<a id="what-key-erasure-guarantees-on-each-key-store"></a>

A successful Cratis Chronicle erasure call confirms that its key-store operations completed; it does not prove that no copy of the key can still decrypt the subject's data. MongoDB deletes documents, HashiCorp Vault deletes secret metadata and versions, and Azure Key Vault requires a purge after soft deletion. Replication, recovery rights and backups affect what remains readable.

Use this page to define what your erasure procedure can promise on those three stores. For the fence mechanism, read [The encryption key lifecycle](key-lifecycle.mdx); for connection settings and permissions, use [Compliance Storage](../hosting/configuration/compliance-storage.md).

## What Chronicle does the same everywhere

Chronicle attempts to record the [erasure fence](key-lifecycle.mdx#the-erasure-fence) in every event store in the namespace before deleting key revisions. It also removes unique-index claims that reserve values for the subject and evicts the key from every silo's cache. A silo is a Chronicle kernel node. The cache wrapper invalidates local entries before and after deletion.

A readable fence blocks automatic provisioning until a new key is authorized and continues to reject destroyed key material afterwards. It does not validate every read. Individual stores can still return stale, restored or recovered material; a composed store checks readable members' fences, but an unavailable member's fence can be missed. Successful deletion therefore depends on the backend, not just the fence.

Stop the subject's appends and forwarding before erasure. Persistent backends check the fence and write keys in separate operations, so an already-running write can persist after deletion. Repeat erasure if writes overlapped it.

Key deletion cannot retract plaintext already delivered, protect values that were never encrypted, or destroy backup and snapshot copies. See [what the fence cannot protect against](key-lifecycle.mdx#not-stopped) and [backup and restore ordering](../hosting/configuration/compliance-storage.md#backup-and-restore-ordering).

## At a glance

| Question | MongoDB (default) | HashiCorp Vault (KV v2) | Azure Key Vault |
| --- | --- | --- | --- |
| What the erase does | Deletes the key documents | Deletes the key metadata and all versions of every revision path | Soft-deletes each revision secret, then purges it |
| Is there an undo operation for that deletion? | No document-level soft delete; replicas and backups need separate assessment | Metadata deletion is not version-level soft delete; snapshots remain separate | Soft-deleted secrets are recoverable until purge or retention expiry |
| What makes the call fail | Driver errors propagate | Vault errors propagate | A purge refused with HTTP 403 throws `EncryptionKeyNotPurged`; other service errors propagate |
| Where a read can still return the key | A replica or restored database that still has the document | Stale reads, replicated clusters or restored snapshots | A recovered secret, or a failed-over vault missing the delete |
| What you must configure | Read/write concerns and backup handling appropriate to your erasure procedure | Storage consistency and read routing for your topology | Purge permission for Chronicle's identity, and no purge protection for immediate purge |

The sections below give the detail behind each column.

## MongoDB

Source: `Source/Kernel/Storage.MongoDB/Encryption/EncryptionKeyStorage.cs`.

The key store is the default. Erasing a subject's key calls `DeleteMany` on the key documents for the identifier in the `(event store, namespace)` key collection. The fence is a separate document in an `encryption-key-erasures` collection for the same event store and namespace.

The delete removes the matching documents rather than marking them as soft-deleted. That operation alone does not establish when every possible reader or replica stops returning them.

Chronicle sets no read or write concern of its own for the key store, so your MongoDB connection string and server defaults apply. Chronicle does not establish a bound for visibility or durability during replica-set failover. Confirm an erasure that coincides with failover and repeat it if needed; [Chronicle issue 4977](https://github.com/Cratis/Chronicle/issues/4977) records the consistency concern.

Inventory backups, `mongodump` files, oplog copies and snapshots that can restore the document. Their retention and restore procedures determine how long a deleted key remains recoverable outside the live collection. This page does not establish erasure behavior for sharded MongoDB.

## HashiCorp Vault

Source: `Source/Kernel/Storage.Vault/VaultEncryptionKeyStorage.cs`.

Chronicle stores each revision at its own path in a KV v2 mount. To erase, it lists the revisions and calls the KV v2 [delete metadata and all versions](https://developer.hashicorp.com/vault/api-docs/secret/kv/kv-v2) endpoint for each path. That endpoint permanently removes the key metadata and all version data, so Vault's own soft-delete and undelete do not apply: Chronicle never uses the version-level `delete`, which is recoverable.

Acknowledgment confirms the metadata-deletion request, not that every topology serves a fresh read. Review Vault's [consistency documentation](https://developer.hashicorp.com/vault/docs/enterprise/consistency) for your deployment:

- Without Enterprise performance standbys, Vault documents the default Consul consistency behavior. [Consul documents](https://developer.hashicorp.com/consul/api-docs/features/consistency) stale values from a follower or a leader during an election, so an immediate read on a Consul-backed Vault can still return the key.
- With Consul storage, [`consistency_mode`](https://developer.hashicorp.com/vault/docs/configuration/storage/consul) accepts `default` or `strong`. Chronicle neither sets nor checks it, and does not establish an end-to-end erasure guarantee for `strong`.
- With Enterprise performance standbys, `X-Vault-Forward: active-node` can direct reads to the active node when `allow_forwarding_via_header` is enabled. Chronicle does not send it; a proxy or Vault agent must add it if your topology requires it. This routing does not solve performance replication.
- With integrated storage and no performance standbys, use Vault's documented consistency behavior; Chronicle adds no read-consistency control.

Vault snapshots, Consul snapshots and replicated clusters need their own retention and recovery procedure.

## Azure Key Vault

Source: `Source/Kernel/Storage.AzureKeyVault/AzureKeyVaultEncryptionKeyStorage.cs`, `EncryptionKeyNotPurged.cs`.

Chronicle stores each revision as a secret named `chronicle-k1--{hash}--{revision}`, where `{hash}` is derived from the event store, namespace and identifier. Erasing a revision does two things:

1. It soft-deletes the secret and waits for the delete to finish.
2. It purges the deleted secret.

Soft-delete alone is not an erasure. Azure Key Vault [keeps a soft-deleted secret recoverable](https://learn.microsoft.com/en-us/azure/key-vault/general/soft-delete-overview) for a retention period of 7 to 90 days (90 by default), and Chronicle treats a key that is only soft-deleted as not erased.

### What you must configure

Give Chronicle's identity permission to purge deleted secrets, in addition to the Get, List, Set and Delete permissions in [Compliance Storage](../hosting/configuration/compliance-storage.md#azure-key-vault). It also needs permission to list deleted secrets so it can finish an earlier incomplete erasure.

If your procedure requires purge at erasure time, use a vault without [purge protection](https://learn.microsoft.com/en-us/azure/key-vault/general/soft-delete-overview). With protection enabled, Azure refuses purge until retention expires. Chronicle does not detect this setting in advance, so the erasure call fails while purge remains blocked.

### Disabled secrets

A key secret that is disabled in the vault counts as present but unavailable. Chronicle does not read it, does not replace it with a new key, and does not reuse its revision number: the operation throws `EncryptionKeyUnavailable`, the metric `chronicle-encryption-key-unavailable` is incremented and one warning without identifiers is logged. A disabled erasure secret is treated the same way, so a disabled fence never reads as no fence. Erasing the subject removes and purges the disabled secret, and the erasure covers its revision.

### What happens when the purge is refused

The vault answers the purge with HTTP 403. The erasure fails with `EncryptionKeyNotPurged`, which names the secret and the status. The state at that point is:

- The secret is soft-deleted, so it is no longer returned by an ordinary read, but anyone allowed to recover secrets can recover it until the retention period ends.
- The erasure is not complete, and Chronicle does not report it as complete.
- Repeating the erasure after you grant the permission completes it. Revisions that the failed attempt soft-deleted are found again through the deleted-secrets list.

The Azure store's read path does not validate returned material against the fence. If someone recovers the secret, that path can return the key and decrypt old ciphertext again. Restrict recovery rights and complete the purge rather than relying on the fence to make a recoverable secret unreadable. [Chronicle issue 4978](https://github.com/Cratis/Chronicle/issues/4978) concerns read-side validation; the current source does not provide it on every path.

### Failover and replication

Key Vault replicates within the region and, [asynchronously](https://learn.microsoft.com/en-us/azure/reliability/reliability-key-vault), to the paired region. After a Microsoft-managed failover, the vault is read-only and can serve the secrets that were replicated before it. A delete or an erasure fence written shortly before the failover can be missing in the secondary, and a read-only vault cannot take the repeat. Repeat the erasure once the primary is back, and verify the fence.

## Writing the guarantee into your procedure

Use Chronicle's result as evidence of the operations it completed. An error from a backend fails the erasure; a partial namespace-wide operation throws `EncryptionKeyErasureIncomplete`. Completed operations are logged at information level and incomplete ones at error level. [What Chronicle records](key-lifecycle.mdx#what-chronicle-records-and-what-is-still-yours) explains the subject binding and the actor information your application must supply.

Document the conditions behind your own erasure commitment:

- The consistency settings and routing used by your key store, including how you confirm erasure after failover.
- Who can recover keys or restore snapshots, how long those copies remain available, and what blocks access until erasures are reapplied after a restore.
- Any retention delay that prevents immediate destruction, such as Azure purge protection, and how it affects what you tell the data subject.

Then follow [Erasing a subject](erasing-a-subject) to handle each namespace, stale read models and copies outside the key store. A successful call is a completion check for Chronicle's operations, not a substitute for that wider procedure.

## See also

| Topic | Description |
|---|---|
| [The encryption key lifecycle](key-lifecycle.mdx) | The fence, and what it refuses |
| [Erasing a subject](erasing-a-subject) | Make the call and check the remaining erasure work |
| [Compliance Storage](../hosting/configuration/compliance-storage.md) | Configure each key store |
