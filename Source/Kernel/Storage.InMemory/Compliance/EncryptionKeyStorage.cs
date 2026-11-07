// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.InMemory.Compliance;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEncryptionKeyStorage"/>.
/// </summary>
public sealed class EncryptionKeyStorage : IEncryptionKeyStorage
{
    readonly InMemoryEncryptionKeyStorage _storage = new();

    /// <inheritdoc/>
    public EncryptionKeyFenceGuarantees FenceGuarantees => _storage.FenceGuarantees;

    /// <inheritdoc/>
    public Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null) =>
        _storage.SaveFor(eventStore, eventStoreNamespace, identifier, key, revision);

    /// <inheritdoc/>
    public Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key) =>
        _storage.GetOrAddFor(eventStore, eventStoreNamespace, identifier, key);

    /// <inheritdoc/>
    public Task<EncryptionKeyAtRevision?> TryGetLatestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        _storage.TryGetLatestFor(eventStore, eventStoreNamespace, identifier);

    /// <inheritdoc/>
    public Task<EncryptionKeyAtRevision> GetOrAddWithRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key) =>
        _storage.GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, key);

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
