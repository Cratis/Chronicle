// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_all_instances;

public class and_all_events_are_subscribed_alongside_an_explicit_from : given.all_dependencies
{
    GetAllInstancesResponse _result;

    void Establish()
    {
        var mapped = new EventType("A", EventTypeGeneration.First);
        var unmapped = new EventType("B", EventTypeGeneration.First);
        var definition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "mixed-all",
            _readModelDefinition.Identifier,
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition> { [mapped] = new(new Dictionary<PropertyPath, string> { ["Name"] = "name" }, PropertyExpression.NotSet, null) },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string> { ["Marker"] = "$eventContext(sequenceNumber)" }, false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            SubscribesToAllEvents: true);
        var projection = Substitute.For<IProjection>();
        projection.GetDefinition().Returns(definition);

        // The grain returns its engine's explicit types, even with SubscribesToAllEvents set.
        projection.GetEventTypes().Returns([mapped]);
        projection.Process(Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns([]);
        _grainFactory.GetGrain<IProjection>(Arg.Any<string>()).Returns(projection);
        var definitions = Substitute.For<IReadModelDefinitionsStorage>();
        definitions.Get(_readModelDefinition.Identifier).Returns(_readModelDefinition);
        _eventStoreStorage.ReadModels.Returns(definitions);
        var sequence = Substitute.For<IEventSequenceStorage>();
        _namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(sequence);
        AppendedEvent[] stored =
        [
            new(EventContext.EmptyWithEventSourceId("source") with { EventType = mapped, SequenceNumber = 1 }, new()),
            new(EventContext.EmptyWithEventSourceId("source") with { EventType = unmapped, SequenceNumber = 2 }, new()),
            new(EventContext.EmptyWithEventSourceId("source") with { EventType = mapped, SequenceNumber = 3 }, new())
        ];
        sequence.GetEventsWithLimit(EventSequenceNumber.First, 3, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(call =>
            {
                var filter = call.Arg<IEnumerable<EventType>>().ToArray();
                var cursor = Substitute.For<IEventCursor>();
                cursor.MoveNext().Returns(true, false);
                cursor.Current.Returns(stored.Where(@event => filter.Length == 0 || filter.Contains(@event.Context.EventType)).ToArray());
                return cursor;
            });
    }

    async Task Because() => _result = await _service.GetAllInstances(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        EventCount = 3
    });

    [Fact] void should_replay_every_event_including_the_unmapped_type() => _result.ProcessedEventsCount.ShouldEqual(3UL);
}
