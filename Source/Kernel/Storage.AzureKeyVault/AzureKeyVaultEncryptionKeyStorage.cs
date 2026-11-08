// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>Stores key material in Azure Key Vault, with a database authority for conditional publication.</summary>
/// <remarks>
/// <para>
/// A durable authority publishes exact secret version ids. The vault itself has no conditional write.
/// Without a durable authority the release-N implementation is retained and advertises no fence guarantees.
/// Secret names, eligible legacy locations and legacy provisioning elections are unchanged.
/// </para>
/// <para>
/// Encryption keys are stored as secrets in Azure Key Vault. Each revision of a key for a given
/// identifier is stored as a distinct secret so that revisions can be retrieved or deleted independently.
/// </para>
/// <para>
/// Secret names follow the pattern <c language="csharp">chronicle-k1--{hash}--{revision}</c> where the hash is the SHA-256 of the
/// event store, the namespace and the identifier - see <see cref="SecretNames"/>. Every distinct address therefore has
/// its own secrets, and the names carry no subject data.
/// </para>
/// <para>
/// Keys that were stored under the earlier <c language="csharp">chronicle--{eventStore}--{namespace}--{identifier}--{revision}</c>
/// names are still read, and removed together with the key, for the addresses whose components are made of lowercase letters, digits and
/// single hyphens (plus the identifiers Chronicle builds for encrypted values). Earlier names are never written.
/// </para>
/// <para>
/// Kernel-internal secrets are named <c language="csharp">chronicle-internal--{eventStore}--{purpose}</c>. A subject key
/// name starts with <c language="csharp">chronicle-k1--</c> (or <c language="csharp">chronicle--</c> for earlier names),
/// while an internal secret name starts with <c language="csharp">chronicle-internal--</c>. No subject key
/// name, erasure name or subject key prefix can therefore equal or enumerate an internal secret name, whatever the
/// event store, namespace or identifier.
/// </para>
/// <para>
/// Authentication is performed using the <see cref="Azure.Core.TokenCredential"/> provided to the
/// <see cref="SecretClient"/> constructor parameter.
/// </para>
/// <para>
/// Azure Key Vault has no conditional write, so first-time provisioning cannot be made atomic by the vault itself.
/// Instead every provisioner writes its candidate as a new version of the same secret, tagged as a provisioning
/// candidate, and every reader and provisioner elects the same winner from the versions: the oldest candidate by
/// creation time, tie-broken ordinally on the version identifier. An election is only final once the winner is
/// older than the election's settle time, which is what stops a concurrent write that lands in the same
/// second from being elected after a provisioner has already returned its own key. A secret whose latest version
/// is not a provisioning candidate - one written by <see cref="SaveFor"/> or before elections existed - is served
/// as its latest version, exactly as before.
/// </para>
/// <para>
/// Deleting a key purges it after the soft delete, because crypto-shredding is only an erasure once the key
/// material cannot be recovered. A vault that refuses the purge - purge protection, or a missing purge permission -
/// fails the deletion loudly with <see cref="EncryptionKeyNotPurged"/>.
/// </para>
/// </remarks>
public class AzureKeyVaultEncryptionKeyStorage : IEncryptionKeyStorage
{
    const string InternalSecretNamePrefix = "chronicle-internal";
    readonly IEncryptionKeyStorage _storage;
    readonly LegacyAzureKeyVaultEncryptionKeyStorage _internalSecrets;

    /// <summary>Initializes a store without a durable authority.</summary>
    /// <param name="secretClient">The material service.</param>
    /// <param name="logger">The logger.</param>
    public AzureKeyVaultEncryptionKeyStorage(SecretClient secretClient, ILogger<AzureKeyVaultEncryptionKeyStorage>? logger = null)
        : this(secretClient, TimeProvider.System, logger)
    {
    }

    /// <summary>Initializes a store using the selected default backend's authority.</summary>
    /// <param name="secretClient">The material service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="fence">The database authority.</param>
    /// <param name="delay">Optional injectable retry delay.</param>
    public AzureKeyVaultEncryptionKeyStorage(SecretClient secretClient, ILogger<AzureKeyVaultEncryptionKeyStorage>? logger, IEncryptionKeyFence fence, Func<TimeSpan, Task>? delay = null)
    {
        var actualLogger = logger ?? NullLogger<AzureKeyVaultEncryptionKeyStorage>.Instance;
        var legacy = new LegacyAzureKeyVaultEncryptionKeyStorage(secretClient, actualLogger);
        _internalSecrets = legacy;
        _storage = fence.IsDurable
            ? new FencedAzureKeyVaultEncryptionKeyStorage(secretClient, fence, legacy, actualLogger, delay)
            : legacy;
        if (!fence.IsDurable) actualLogger.NoDurableAuthority();
    }

    /// <summary>Initializes legacy elections with an injectable clock.</summary>
    /// <param name="secretClient">The material service.</param>
    /// <param name="timeProvider">The election clock.</param>
    /// <param name="logger">The logger.</param>
    internal AzureKeyVaultEncryptionKeyStorage(SecretClient secretClient, TimeProvider timeProvider, ILogger<AzureKeyVaultEncryptionKeyStorage>? logger = null)
    {
        _internalSecrets = new LegacyAzureKeyVaultEncryptionKeyStorage(secretClient, timeProvider, logger);
        _storage = _internalSecrets;
    }

    /// <inheritdoc/>
    public IReadOnlyList<EncryptionKeyStorageCapability> Capabilities => _storage.Capabilities;

    /// <inheritdoc/>
    public EncryptionKeyFenceGuarantees FenceGuarantees => _storage.FenceGuarantees;

    /// <inheritdoc/>
    public Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null) =>
        _storage.SaveFor(eventStore, eventStoreNamespace, identifier, key, revision);

    /// <inheritdoc/>
    public Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key) =>
        _storage.GetOrAddFor(eventStore, eventStoreNamespace, identifier, key);

    /// <inheritdoc/>
    public Task<EncryptionKeyAtRevision> GetOrAddWithRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key) =>
        _storage.GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, key);

    /// <inheritdoc/>
    public Task<EncryptionKeyAtRevision?> TryGetLatestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        _storage.TryGetLatestFor(eventStore, eventStoreNamespace, identifier);

    /// <inheritdoc/>
    public Task<EncryptionKeyAtRevision?> TryGetAtRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision revision) =>
        _storage.TryGetAtRevisionFor(eventStore, eventStoreNamespace, identifier, revision);

    /// <inheritdoc/>
    public Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        _storage.HasFor(eventStore, eventStoreNamespace, identifier, revision);

    /// <inheritdoc/>
    public Task<EncryptionKey?> TryGetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        _storage.TryGetFor(eventStore, eventStoreNamespace, identifier, revision);

    /// <inheritdoc/>
    public Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        _storage.GetFor(eventStore, eventStoreNamespace, identifier, revision);

    /// <inheritdoc/>
    public Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        _storage.DeleteFor(eventStore, eventStoreNamespace, identifier, revision);

    /// <inheritdoc/>
    public Task<EncryptionKeyErasure?> GetErasureForRelease(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        _storage.GetErasureForRelease(eventStore, eventStoreNamespace, identifier);

    /// <inheritdoc/>
    public Task<EncryptionKeyErasure?> GetErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        _storage.GetErasureFor(eventStore, eventStoreNamespace, identifier);

    /// <inheritdoc/>
    public Task<EncryptionKeyErasureRequest?> GetErasureRequestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId) =>
        _storage.GetErasureRequestFor(eventStore, eventStoreNamespace, identifier, requestId);

    /// <inheritdoc/>
    public Task CompactErasureRequestsFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, int retain = 32) =>
        _storage.CompactErasureRequestsFor(eventStore, eventStoreNamespace, identifier, retain);

    /// <inheritdoc/>
    /// <remarks>Internal secrets keep the same isolated naming and candidate election with or without a fence authority.</remarks>
    public Task<EncryptionKey> GetOrAddInternalSecretFor(EventStoreName eventStore, InternalSecretPurpose purpose, EncryptionKey candidate) =>
        _internalSecrets.GetOrAddInternalSecretFor(eventStore, purpose, candidate);

    /// <inheritdoc/>
    public Task<EncryptionKeyErasure?> GetFenceFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        _storage.GetFenceFor(eventStore, eventStoreNamespace, identifier);

    /// <inheritdoc/>
    public Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        _storage.RecordErasureFor(eventStore, eventStoreNamespace, identifier);

    /// <inheritdoc/>
    public Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyErasureRequest request) =>
        _storage.RecordErasureFor(eventStore, eventStoreNamespace, identifier, request);

    /// <inheritdoc/>
    public Task DeleteThroughFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId) =>
        _storage.DeleteThroughFor(eventStore, eventStoreNamespace, identifier, requestId);

    /// <inheritdoc/>
    public Task ConfirmErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId) =>
        _storage.ConfirmErasureFor(eventStore, eventStoreNamespace, identifier, requestId);

    /// <inheritdoc/>
    public Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        _storage.AllowNewKeyFor(eventStore, eventStoreNamespace, identifier);

    /// <inheritdoc/>
    public Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, ulong observedVersion, uint observedLifecycle) =>
        _storage.AllowNewKeyFor(eventStore, eventStoreNamespace, identifier, observedVersion, observedLifecycle);

    /// <summary>
    /// Builds the secret name of a kernel-internal secret.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="purpose">The <see cref="InternalSecretPurpose"/>.</param>
    /// <returns>The secret name.</returns>
    internal static string BuildInternalSecretName(EventStoreName eventStore, InternalSecretPurpose purpose) =>
        $"{InternalSecretNamePrefix}--{SecretNames.InternalSecretHash(eventStore)}--{purpose.Value}";
}
