// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model.given;

/// <summary>
/// A projection-backed read model whose instance was shaped by four events, appended under two
/// correlations - so the two groupings produce visibly different numbers of snapshots.
/// </summary>
public class a_projection_with_a_history : Specification
{
    protected static readonly CorrelationId FirstCorrelation = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    protected static readonly CorrelationId SecondCorrelation = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected IEventCompliance _eventCompliance;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected JsonSerializerOptions _jsonSerializerOptions;
    protected IProjection _projection;
    protected int _foldCount;

    void Establish()
    {
        var eventType = new EventType("my-event", 1);

        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _eventCompliance = Substitute.For<IEventCompliance>();
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _jsonSerializerOptions = new JsonSerializerOptions();

        var readModelDefinition = new ReadModelDefinition(
            "test-read-model",
            "test-container",
            "Test Read Model",
            ReadModelOwner.None,
            ReadModelSource.Unknown,
            ReadModelObserverType.Projection,
            "my-projection",
            new SinkDefinition(SinkConfigurationId.None, SinkTypeId.None),
            new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, new JsonSchema() } },
            []);

        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceStorage);

        var readModel = Substitute.For<IReadModel>();
        readModel.GetDefinition().Returns(readModelDefinition);
        _grainFactory.GetGrain<IReadModel>(Arg.Any<string>()).Returns(readModel);

        var readModelDefinitions = Substitute.For<IReadModelDefinitionsStorage>();
        eventStoreStorage.ReadModels.Returns(readModelDefinitions);
        readModelDefinitions.Get(Arg.Any<ReadModelIdentifier>()).Returns(readModelDefinition);

        _projection = Substitute.For<IProjection>();
        _grainFactory.GetGrain<IProjection>(Arg.Any<string>()).Returns(_projection);
        _projection.GetEventTypes().Returns([eventType]);

        // An empty key on the from definition means the instance is keyed by its own event source, which
        // is the path that reads the sequence for one event source rather than filtering the whole log.
        _projection.GetDefinition().Returns(new ProjectionDefinition(
            ProjectionOwner.None,
            Concepts.EventSequences.EventSequenceId.Log,
            new ProjectionId("my-projection"),
            readModelDefinition.Identifier,
            true,
            true,
            new JsonObject(),
            new Dictionary<EventType, FromDefinition>
            {
                [eventType] = new(new Dictionary<PropertyPath, string>(), new PropertyExpression(string.Empty), null)
            },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>()));

        var events = new List<AppendedEvent>
        {
            EventAt(1, FirstCorrelation),
            EventAt(2, FirstCorrelation),
            EventAt(3, SecondCorrelation),
            EventAt(4, SecondCorrelation)
        };

        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(true, false);
        cursor.Current.Returns(events);

        var eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        eventSequenceStorage
            .GetFromSequenceNumber(EventSequenceNumber.First, Arg.Any<EventSourceId>(), eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(cursor);
        namespaceStorage.GetEventSequence("event-log").Returns(eventSequenceStorage);

        var eventTypesStorage = Substitute.For<IEventTypesStorage>();
        eventStoreStorage.EventTypes.Returns(eventTypesStorage);
        eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns([new EventTypeSchema(eventType, EventTypeOwner.Server, EventTypeSource.Code, new JsonSchema())]);

        _eventCompliance
            .Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(call => Task.FromResult(call.ArgAt<IEnumerable<AppendedEvent>>(0).ToArray()));

        // Counted so a spec can show the fold is one pass whichever grouping is asked for.
        _projection
            .ProcessForSingleReadModel(Arg.Any<EventStoreNamespaceName>(), Arg.Any<ExpandoObject>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call =>
            {
                _foldCount++;
                return Task.FromResult(call.ArgAt<ExpandoObject>(1));
            });

        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(new JsonObject());
    }

    protected Task<IEnumerable<ReadModelSnapshot>> AllSnapshots(string grouping = nameof(ReadModelSnapshotGrouping.Correlation)) =>
        ReadModelSnapshot.AllSnapshotsForReadModel(
            _grainFactory,
            _storage,
            _eventCompliance,
            _expandoObjectConverter,
            _jsonSerializerOptions,
            "test-store",
            "test-namespace",
            "test-read-model",
            "my-instance",
            "event-log",
            grouping);

    static AppendedEvent EventAt(ulong sequenceNumber, CorrelationId correlationId) =>
        new(
            Concepts.Events.EventContext.EmptyWithEventSourceId("my-instance") with
            {
                SequenceNumber = sequenceNumber,
                CorrelationId = correlationId,
                Occurred = DateTimeOffset.UnixEpoch.AddMinutes(sequenceNumber)
            },
            new ExpandoObject());
}
