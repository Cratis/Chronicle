// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_ReactorsReactor.given;

public class a_reactors_reactor : Specification
{
    protected ReactorsReactor _reactor;
    protected IReactors _reactors;
    protected IEventTypes _eventTypes;
    protected IStorage _storage;
    protected IEventStoreNamespaceStorage _namespaceStorage;

    void Establish()
    {
        _reactors = Substitute.For<IReactors>();
        _eventTypes = Substitute.For<IEventTypes>();
        _storage = Substitute.For<IStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _namespaceStorage.HasData().Returns(Task.FromResult(true));

        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);

        _reactor = new ReactorsReactor(_reactors, _eventTypes, _storage);
    }
}
