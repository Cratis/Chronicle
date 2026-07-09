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
    readonly Lock _lock = new();

    /// <inheritdoc/>
    public Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null)
    {
        lock (_lock)
        {
            var actualRevision = IsLatest(revision) ? GetNextRevision(eventStore, eventStoreNamespace, identifier) : revision!;
            _keys[new(eventStore, eventStoreNamespace, identifier, actualRevision)] = key;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        lock (_lock)
        {
            if (TryGetLatest(eventStore, eventStoreNamespace, identifier) is { } existing)
            {
                return Task.FromResult(existing);
            }

            _keys[new(eventStore, eventStoreNamespace, identifier, EncryptionKeyRevision.Initial)] = key;
            return Task.FromResult(key);
        }
    }

    /// <inheritdoc/>
    public Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        lock (_lock)
        {
            if (IsLatest(revision))
            {
                return Task.FromResult(_keys.Keys.Any(k => k.EventStore == eventStore && k.EventStoreNamespace == eventStoreNamespace && k.Identifier == identifier));
            }

            return Task.FromResult(_keys.ContainsKey(new(eventStore, eventStoreNamespace, identifier, revision!)));
        }
    }

    /// <inheritdoc/>
    public Task<EncryptionKey?> TryGetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        lock (_lock)
        {
            if (IsLatest(revision))
            {
                return Task.FromResult(TryGetLatest(eventStore, eventStoreNamespace, identifier));
            }

            return Task.FromResult(_keys.TryGetValue(new(eventStore, eventStoreNamespace, identifier, revision!), out var key) ? key : null);
        }
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) ?? throw new MissingEncryptionKey(identifier);

    /// <inheritdoc/>
    public Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        lock (_lock)
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
        }

        return Task.CompletedTask;
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    EncryptionKey? TryGetLatest(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        _keys
            .Where(kv => kv.Key.EventStore == eventStore && kv.Key.EventStoreNamespace == eventStoreNamespace && kv.Key.Identifier == identifier)
            .OrderByDescending(kv => kv.Key.Revision.Value)
            .Select(kv => (EncryptionKey?)kv.Value)
            .FirstOrDefault();

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
