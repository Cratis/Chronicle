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
/// A durable authority publishes exact secret version ids. The vault itself has no conditional write.
/// Without a durable authority the release-N implementation is retained and advertises no fence guarantees.
/// Secret names, eligible legacy locations and legacy provisioning elections are unchanged.
/// </remarks>
public class AzureKeyVaultEncryptionKeyStorage : IEncryptionKeyStorage
{
    readonly IEncryptionKeyStorage _storage;

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
        _storage = fence.IsDurable
            ? new FencedAzureKeyVaultEncryptionKeyStorage(secretClient, fence, legacy, actualLogger, delay)
            : legacy;
        if (!fence.IsDurable) actualLogger.NoDurableAuthority();
    }

    /// <summary>Initializes legacy elections with an injectable clock.</summary>
    /// <param name="secretClient">The material service.</param>
    /// <param name="timeProvider">The election clock.</param>
    /// <param name="logger">The logger.</param>
    internal AzureKeyVaultEncryptionKeyStorage(SecretClient secretClient, TimeProvider timeProvider, ILogger<AzureKeyVaultEncryptionKeyStorage>? logger = null) =>
        _storage = new LegacyAzureKeyVaultEncryptionKeyStorage(secretClient, timeProvider, logger);

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
    public Task<EncryptionKeyErasure?> GetErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        _storage.GetErasureFor(eventStore, eventStoreNamespace, identifier);

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
}
