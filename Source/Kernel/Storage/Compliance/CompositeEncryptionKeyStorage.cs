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
    public async Task<EncryptionKey> GetFor(EncryptionKeyIdentifier identifier)
    {
        IEncryptionKeyStorage? store = default;

        foreach (var innerStore in _inner)
        {
            if (await innerStore.HasFor(identifier))
            {
                store = innerStore;
            }
        }

        if (store != default)
        {
            var key = await store.GetFor(identifier);
            foreach (var storeToSaveIn in _inner.Where(_ => _ != store))
            {
                await storeToSaveIn.SaveFor(identifier, key);
            }

            return key;
        }

        throw new MissingEncryptionKey(identifier);
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(EncryptionKeyIdentifier identifier)
    {
        foreach (var innerStore in _inner)
        {
            if (await innerStore.HasFor(identifier))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task SaveFor(EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        foreach (var innerStore in _inner)
        {
            await innerStore.SaveFor(identifier, key);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteFor(EncryptionKeyIdentifier identifier)
    {
        foreach (var innerStore in _inner)
        {
            await innerStore.DeleteFor(identifier);
        }
    }
}
