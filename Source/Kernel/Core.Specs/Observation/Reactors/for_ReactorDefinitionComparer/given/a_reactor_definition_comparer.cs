// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.for_ReactorDefinitionComparer.given;

public class a_reactor_definition_comparer : Specification
{
    protected ReactorDefinitionComparer _comparer;
    protected IReactorDefinitionsStorage _reactorsStorage;
    protected ReactorKey _key;
    protected ReactorDefinition _first;
    protected ReactorDefinition _second;
    protected ReactorDefinitionCompareResult _result;

    void Establish()
    {
        _key = new("the-reactor", "the-event-store", EventStoreNamespaceName.Default, EventSequenceId.Log);
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _reactorsStorage = Substitute.For<IReactorDefinitionsStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        eventStoreStorage.Reactors.Returns(_reactorsStorage);
        _reactorsStorage.Has(_key.ReactorId).Returns(true);
        _comparer = new(storage);
        _first = DefinitionWith(new("FirstEvent", EventTypeGeneration.First), new("SecondEvent", EventTypeGeneration.First));
        _second = _first with { EventTypes = _first.EventTypes.ToArray() };
    }

    protected static ReactorDefinition DefinitionWith(params EventType[] eventTypes) =>
        new("the-reactor", ReactorOwner.Client, EventSequenceId.Log, eventTypes.Select(_ => new Concepts.Observation.EventTypeWithKeyExpression(_, "$.id")).ToArray());

    protected static ReactorDefinition EmptyState() => DefinitionWith() with { EventTypes = null! };

    protected async Task Compare() => _result = await _comparer.Compare(_key, _first, _second);
}
