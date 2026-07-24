// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_all_instances;

public class and_the_event_cursor_fails : given.all_dependencies
{
    IEventCursor _cursor = null!;
    Exception _error = null!;

    void Establish()
    {
        var readModelDefinitionsStorage = Substitute.For<IReadModelDefinitionsStorage>();
        _eventStoreStorage.ReadModels.Returns(readModelDefinitionsStorage);
        readModelDefinitionsStorage.Get(_readModelDefinition.Identifier).Returns(_readModelDefinition);

        var eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _namespaceStorage.GetEventSequence("event-log").Returns(eventSequenceStorage);

        var projection = Substitute.For<IProjection>();
        projection.GetEventTypes().Returns([]);
        _grainFactory.GetGrain<IProjection>(Arg.Any<string>()).Returns(projection);

        _cursor = Substitute.For<IEventCursor>();
        _cursor.When(_ => _.MoveNext()).Do(_ => throw new Exception("cursor failure"));
        eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: null, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(_cursor);
    }

    async Task Because() => _error = await Catch.Exception(() => _service.GetAllInstances(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        EventCount = ulong.MaxValue
    }));

    [Fact] void should_propagate_the_failure() => _error.ShouldNotBeNull();
    [Fact] void should_dispose_the_cursor() => _cursor.Received().Dispose();
}
