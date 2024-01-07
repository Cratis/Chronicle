// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;

namespace Aksio.Cratis.Kernel.Storage.Compliance;

/// <summary>
/// Represents a composite <see cref="IEncryptionKeyStorage"/>.
/// </summary>
public class CompositeEncryptionKeyStorage : IEncryptionKeyStorage
{
    readonly IEncryptionKeyStorage[] _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeEncryptionKeyStorage"/>.
    /// </summary>
    /// <param name="inner">Inner collection of <see cref="IEncryptionKeyStorage"/>.</param>
    public CompositeEncryptionKeyStorage(params IEncryptionKeyStorage[] inner)
    {
        _inner = inner;
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        IEncryptionKeyStorage? store = default;

        foreach (var innerStore in _inner)
        {
            if (await innerStore.HasFor(eventStore, eventStoreNamespace, identifier))
            {
                store = innerStore;
            }
        }

        if (store != default)
        {
            var key = await store.GetFor(eventStore, eventStoreNamespace, identifier);
            foreach (var storeToSaveIn in _inner.Where(_ => _ != store))
            {
                await storeToSaveIn.SaveFor(eventStore, eventStoreNamespace, identifier, key);
            }

            return key;
        }

        throw new MissingEncryptionKey(identifier);
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        foreach (var innerStore in _inner)
        {
            if (await innerStore.HasFor(eventStore, eventStoreNamespace, identifier))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        foreach (var innerStore in _inner)
        {
            await innerStore.SaveFor(eventStore, eventStoreNamespace, identifier, key);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        foreach (var innerStore in _inner)
        {
            await innerStore.DeleteFor(eventStore, eventStoreNamespace, identifier);
        }
    }
}
