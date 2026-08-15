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
    readonly ConcurrentDictionary<Scope, EncryptionKeyErasure> _erasures = new();

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

        ErasureFor(eventStore, eventStoreNamespace, identifier).EnsureCanSave(identifier, actualRevision, key);

        _keys[new Key(eventStore, eventStoreNamespace, identifier, actualRevision)] = key;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<EncryptionKey> GetOrAddFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKey key)
    {
        if (TryGetLatest(eventStore, eventStoreNamespace, identifier) is { } existing)
        {
            return Task.FromResult(existing);
        }

        var revision = ErasureFor(eventStore, eventStoreNamespace, identifier).RevisionForNewKey(identifier, key);
        var added = _keys.GetOrAdd(new Key(eventStore, eventStoreNamespace, identifier, revision), key);
        return Task.FromResult(added);
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
    public Task<EncryptionKey?> TryGetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        if (IsLatest(revision))
        {
            return Task.FromResult(TryGetLatest(eventStore, eventStoreNamespace, identifier));
        }

        return Task.FromResult(_keys.TryGetValue(new Key(eventStore, eventStoreNamespace, identifier, revision!), out var found) ? found : null);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) ?? throw new MissingEncryptionKey(identifier);

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

    /// <inheritdoc/>
    public Task<EncryptionKeyErasure?> GetErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier) =>
        Task.FromResult(ErasureFor(eventStore, eventStoreNamespace, identifier));

    /// <inheritdoc/>
    public Task RecordErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var scope = new Scope(eventStore, eventStoreNamespace, identifier);
        var present = KeysFor(eventStore, eventStoreNamespace, identifier).Select(_ => (_.Key.Revision, _.Value));
        _erasures[scope] = EncryptionKeyErasure.Covering(_erasures.GetValueOrDefault(scope), present);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task AllowNewKeyFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var scope = new Scope(eventStore, eventStoreNamespace, identifier);
        if (_erasures.TryGetValue(scope, out var erasure))
        {
            _erasures[scope] = erasure with { NewKeyAllowed = true };
        }

        return Task.CompletedTask;
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    EncryptionKeyErasure? ErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier) =>
        _erasures.GetValueOrDefault(new Scope(eventStore, eventStoreNamespace, identifier));

    IEnumerable<KeyValuePair<Key, EncryptionKey>> KeysFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier) =>
        _keys.Where(_ =>
            _.Key.EventStore == eventStore &&
            _.Key.EventStoreNamespace == eventStoreNamespace &&
            _.Key.Identifier == identifier);

    EncryptionKey? TryGetLatest(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier) =>
        KeysFor(eventStore, eventStoreNamespace, identifier)
            .OrderByDescending(_ => _.Key.Revision.Value)
            .Select(_ => (EncryptionKey?)_.Value)
            .FirstOrDefault();

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
    /// Represents the identity an erasure is recorded against.
    /// </summary>
    /// <param name="EventStore">The <see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="EventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="Identifier">The <see cref="EncryptionKeyIdentifier"/>.</param>
    sealed record Scope(
        EventStoreName EventStore,
        EventStoreNamespaceName EventStoreNamespace,
        EncryptionKeyIdentifier Identifier);

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
