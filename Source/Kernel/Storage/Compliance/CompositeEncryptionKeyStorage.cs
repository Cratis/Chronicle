// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Represents a composite <see cref="IEncryptionKeyStorage"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CompositeEncryptionKeyStorage"/>.
/// </remarks>
/// <param name="inner">Inner collection of <see cref="IEncryptionKeyStorage"/>.</param>
public class CompositeEncryptionKeyStorage(params IEncryptionKeyStorage[] inner) : IEncryptionKeyStorage
{
    /// <inheritdoc/>
    public async Task<EncryptionKey?> TryGetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        IEncryptionKeyStorage? store = default;

        foreach (var innerStore in inner)
        {
            if (await innerStore.HasFor(eventStore, eventStoreNamespace, identifier, revision))
            {
                store = innerStore;
            }
        }

        if (store == default)
        {
            return null;
        }

        var key = await store.GetFor(eventStore, eventStoreNamespace, identifier, revision);
        foreach (var storeToSaveIn in inner.Where(_ => _ != store))
        {
            await storeToSaveIn.SaveFor(eventStore, eventStoreNamespace, identifier, key, revision);
        }

        return key;
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) ?? throw new MissingEncryptionKey(identifier);

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        if (inner.Length == 0)
        {
            return key;
        }

        // Provision atomically on the first store, then mirror the winning key to the rest so every store
        // converges on the same key pair. Using GetOrAddFor on the mirrors keeps them idempotent.
        var provisioned = await inner[0].GetOrAddFor(eventStore, eventStoreNamespace, identifier, key);
        foreach (var store in inner.Skip(1))
        {
            await store.GetOrAddFor(eventStore, eventStoreNamespace, identifier, provisioned);
        }

        return provisioned;
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        foreach (var innerStore in inner)
        {
            if (await innerStore.HasFor(eventStore, eventStoreNamespace, identifier, revision))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null)
    {
        foreach (var innerStore in inner)
        {
            await innerStore.SaveFor(eventStore, eventStoreNamespace, identifier, key, revision);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        foreach (var innerStore in inner)
        {
            await innerStore.DeleteFor(eventStore, eventStoreNamespace, identifier, revision);
        }
    }
}
