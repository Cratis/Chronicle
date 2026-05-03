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
    public Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null)
    {
        var actualRevision = IsLatest(revision) ? GetNextRevision(eventStore, eventStoreNamespace, identifier) : revision!;
        _keys[new(eventStore, eventStoreNamespace, identifier, actualRevision)] = key;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        if (IsLatest(revision))
        {
            return Task.FromResult(_keys.Keys.Any(k => k.EventStore == eventStore && k.EventStoreNamespace == eventStoreNamespace && k.Identifier == identifier));
        }

        return Task.FromResult(_keys.ContainsKey(new(eventStore, eventStoreNamespace, identifier, revision!)));
    }

    /// <inheritdoc/>
    public Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        if (IsLatest(revision))
        {
            var entry = _keys
                .Where(kv => kv.Key.EventStore == eventStore && kv.Key.EventStoreNamespace == eventStoreNamespace && kv.Key.Identifier == identifier)
                .OrderByDescending(kv => kv.Key.Revision.Value)
                .First();
            return Task.FromResult(entry.Value);
        }

        return Task.FromResult(_keys[new(eventStore, eventStoreNamespace, identifier, revision!)]);
    }

    /// <inheritdoc/>
    public Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        if (IsLatest(revision))
        {
            var keysToRemove = _keys.Keys
                .Where(k => k.EventStore == eventStore && k.EventStoreNamespace == eventStoreNamespace && k.Identifier == identifier)
                .ToList();
            foreach (var key in keysToRemove)
            {
                _keys.Remove(key);
            }
        }
        else
        {
            _keys.Remove(new(eventStore, eventStoreNamespace, identifier, revision!));
        }

        return Task.CompletedTask;
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    EncryptionKeyRevision GetNextRevision(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var maxRevision = _keys.Keys
            .Where(k => k.EventStore == eventStore && k.EventStoreNamespace == eventStoreNamespace && k.Identifier == identifier)
            .Select(k => k.Revision.Value)
            .DefaultIfEmpty(0u)
            .Max();
        return maxRevision + 1u;
    }

    sealed record Key(EventStoreName EventStore, EventStoreNamespaceName EventStoreNamespace, EncryptionKeyIdentifier Identifier, EncryptionKeyRevision Revision);
}
