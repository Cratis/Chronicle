// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.given;

public class two_key_stores : Specification
{
    protected InMemoryEncryptionKeyStorage _primary;
    protected InMemoryEncryptionKeyStorage _secondary;
    protected CompositeEncryptionKeyStorage _composite;
    protected EncryptionKeyIdentifier _identifier;

    void Establish()
    {
        _primary = new InMemoryEncryptionKeyStorage();
        _secondary = new InMemoryEncryptionKeyStorage();
        _composite = new CompositeEncryptionKeyStorage(_primary, _secondary);
        _identifier = new EncryptionKeyIdentifier(Guid.NewGuid().ToString());
    }

    protected static EncryptionKey KeyNamed(string name) =>
        new(Encoding.UTF8.GetBytes($"{name}-public"), Encoding.UTF8.GetBytes($"{name}-private"));

    protected static IEncryptionKeyStorage AnUnreachableStore()
    {
        var store = Substitute.For<IEncryptionKeyStorage>();
        store.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(new StoreUnreachable());
        store.GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(new StoreUnreachable());
        store.HasFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(new StoreUnreachable());
        store.GetOrAddFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKey>()).ThrowsAsync(new StoreUnreachable());
        store.SaveFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKey>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(new StoreUnreachable());
        store.DeleteFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(new StoreUnreachable());
        return store;
    }

    protected Task<EncryptionKey?> KeyIn(IEncryptionKeyStorage store, EncryptionKeyRevision? revision = null) =>
        store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, revision);

    protected Task<bool> HasKeyIn(IEncryptionKeyStorage store, EncryptionKeyRevision? revision = null) =>
        store.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, revision);

    protected Task Save(IEncryptionKeyStorage store, EncryptionKey key, EncryptionKeyRevision? revision = null) =>
        store.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, key, revision);

    protected async Task<int> RevisionCountIn(IEncryptionKeyStorage store)
    {
        var count = 0;
        for (var revision = 1u; revision <= 8; revision++)
        {
            if (await store.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, new EncryptionKeyRevision(revision)))
            {
                count++;
            }
        }

        return count;
    }
}
