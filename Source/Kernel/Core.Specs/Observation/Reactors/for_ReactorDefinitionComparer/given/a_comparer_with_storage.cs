// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.for_ReactorDefinitionComparer.given;

public class a_comparer_with_storage : Specification
{
    protected static readonly ReactorKey _reactorKey = new("the-reactor", "the-event-store", "the-namespace", EventSequenceId.Log);

    protected IReactorDefinitionsStorage _reactorDefinitionsStorage;
    protected IReactorDefinitionComparer _comparer;

    void Establish()
    {
        _reactorDefinitionsStorage = Substitute.For<IReactorDefinitionsStorage>();

        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.Reactors.Returns(_reactorDefinitionsStorage);

        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);

        _comparer = new ReactorDefinitionComparer(storage);
    }

    protected static ReactorDefinition DefinitionWithEventTypes(params EventTypeId[] eventTypeIds) => new(
        _reactorKey.ReactorId,
        ReactorOwner.Client,
        _reactorKey.EventSequenceId,
        eventTypeIds.Select(id => new EventTypeWithKeyExpression(new EventType(id, EventTypeGeneration.First), new PropertyExpression(string.Empty))));

    protected static ReactorDefinition EmptyDefinition() => new(
        _reactorKey.ReactorId,
        ReactorOwner.Client,
        _reactorKey.EventSequenceId,
        EventTypes: null!);
}
