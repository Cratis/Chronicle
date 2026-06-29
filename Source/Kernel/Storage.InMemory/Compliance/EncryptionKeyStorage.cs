// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.InMemory.Compliance;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEncryptionKeyStorage"/>.
/// </summary>
public sealed class EncryptionKeyStorage : IEncryptionKeyStorage
{
    readonly ConcurrentDictionary<Key, EncryptionKey> _keys = new();

    /// <inheritdoc/>
    public Task SaveFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKey key,
        EncryptionKeyRevision? revision = null)
    {
        var actualRevision = IsLatest(revision)
            ? GetNextRevision(eventStore, eventStoreNamespace, identifier)
            : revision!;

        _keys[new Key(eventStore, eventStoreNamespace, identifier, actualRevision)] = key;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> HasFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        if (IsLatest(revision))
        {
            return Task.FromResult(KeysFor(eventStore, eventStoreNamespace, identifier).Any());
        }

        return Task.FromResult(_keys.ContainsKey(new Key(eventStore, eventStoreNamespace, identifier, revision!)));
    }

    /// <inheritdoc/>
    public Task<EncryptionKey> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        EncryptionKey? key;
        if (IsLatest(revision))
        {
            key = KeysFor(eventStore, eventStoreNamespace, identifier)
                .OrderByDescending(_ => _.Key.Revision.Value)
                .Select(_ => _.Value)
                .FirstOrDefault();
        }
        else
        {
            key = _keys.TryGetValue(new Key(eventStore, eventStoreNamespace, identifier, revision!), out var found) ? found : null;
        }

        if (key is null)
        {
            throw new MissingEncryptionKey(identifier);
        }

        return Task.FromResult(key);
    }

    /// <inheritdoc/>
    public Task DeleteFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        if (IsLatest(revision))
        {
            foreach (var entry in KeysFor(eventStore, eventStoreNamespace, identifier).ToArray())
            {
                _keys.TryRemove(entry.Key, out _);
            }
        }
        else
        {
            _keys.TryRemove(new Key(eventStore, eventStoreNamespace, identifier, revision!), out _);
        }

        return Task.CompletedTask;
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    IEnumerable<KeyValuePair<Key, EncryptionKey>> KeysFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier) =>
        _keys.Where(_ =>
            _.Key.EventStore == eventStore &&
            _.Key.EventStoreNamespace == eventStoreNamespace &&
            _.Key.Identifier == identifier);

    EncryptionKeyRevision GetNextRevision(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var latest = KeysFor(eventStore, eventStoreNamespace, identifier)
            .Select(_ => _.Key.Revision.Value)
            .DefaultIfEmpty(0u)
            .Max();

        return latest + 1u;
    }

    /// <summary>
    /// Represents the composite key for a stored encryption key.
    /// </summary>
    /// <param name="EventStore">The <see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="EventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="Identifier">The <see cref="EncryptionKeyIdentifier"/>.</param>
    /// <param name="Revision">The <see cref="EncryptionKeyRevision"/>.</param>
    sealed record Key(
        EventStoreName EventStore,
        EventStoreNamespaceName EventStoreNamespace,
        EncryptionKeyIdentifier Identifier,
        EncryptionKeyRevision Revision);
}
