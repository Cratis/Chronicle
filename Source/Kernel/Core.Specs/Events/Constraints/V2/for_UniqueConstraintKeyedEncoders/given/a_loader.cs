// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintKeyedEncoders.given;

public class a_loader : Specification
{
    protected IStorage _storage;
    private protected UniqueConstraintKeyedEncoders _loader;
    protected Dictionary<EventStoreName, IUniqueConstraintEncodingStorage> _keyStorages;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _keyStorages = [];
        _loader = new UniqueConstraintKeyedEncoders(_storage);
    }

    protected IUniqueConstraintEncodingStorage KeyStorageFor(EventStoreName eventStore, UniqueConstraintEncodingKey? key)
    {
        var keyStorage = Substitute.For<IUniqueConstraintEncodingStorage>();
        keyStorage.GetKey().Returns(Task.FromResult(key));
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.UniqueConstraintEncoding.Returns(keyStorage);
        _storage.GetEventStore(eventStore).Returns(eventStoreStorage);
        _keyStorages[eventStore] = keyStorage;
        return keyStorage;
    }
}
