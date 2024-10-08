// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStorage"/> for in-memory.
/// </summary>
[Singleton]
public class InMemoryEncryptionKeyStorage : IEncryptionKeyStorage
{
    readonly Dictionary<Key, EncryptionKey> _keys = [];

    /// <inheritdoc/>
    public Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        _keys[new(eventStore, eventStoreNamespace, identifier)] = key;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        Task.FromResult(_keys.ContainsKey(new(eventStore, eventStoreNamespace, identifier)));

    /// <inheritdoc/>
    public Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        Task.FromResult(_keys[new(eventStore, eventStoreNamespace, identifier)]);

    /// <inheritdoc/>
    public Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        _keys.Remove(new(eventStore, eventStoreNamespace, identifier));
        return Task.CompletedTask;
    }

    sealed record Key(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier);
}
