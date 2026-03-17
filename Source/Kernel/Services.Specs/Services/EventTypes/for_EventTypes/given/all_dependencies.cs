// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using KernelEventTypes = Cratis.Chronicle.Services.Events.EventTypes;

namespace Cratis.Chronicle.Services.Events.for_EventTypes.given;

public class all_dependencies : Specification
{
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventTypesStorage _eventTypesStorage;
    protected IGrainFactory _grainFactory;
    protected IEventSequence _systemEventSequence;
    protected IEventTypes _subject;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _systemEventSequence = Substitute.For<IEventSequence>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.EventTypes.Returns(_eventTypesStorage);
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_systemEventSequence);
        _subject = new KernelEventTypes(_storage, _grainFactory);
    }
}
