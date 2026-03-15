// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using KernelEventTypes = Cratis.Chronicle.Services.Events.EventTypes;

namespace Cratis.Chronicle.Services.Events.for_EventTypes.given;

public class all_dependencies : Specification
{
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventTypesStorage _eventTypesStorage;
    protected IEventTypes _subject;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.EventTypes.Returns(_eventTypesStorage);
        _subject = new KernelEventTypes(_storage);
    }
}
