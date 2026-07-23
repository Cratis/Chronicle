// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.EventSequences.for_EventSequences.given;

public class all_dependencies : Specification
{
    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IEventCompliance _eventCompliance;
    protected Contracts.EventSequences.IEventSequences _eventSequences;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _eventCompliance = Substitute.For<IEventCompliance>();

        _storage.GetEventStore(Arg.Any<Concepts.EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<Concepts.EventStoreNamespaceName>()).Returns(_namespaceStorage);

        _eventSequences = new EventSequences(_grainFactory, _storage, _eventCompliance, new JsonSerializerOptions());
    }
}
