// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.given;

public class all_dependencies : Specification
{
    protected static readonly EventStoreName EventStore = "test-store";
    protected static readonly EventStoreNamespaceName EventStoreNamespace = "test-namespace";
    protected const string EventSourceIdValue = "event-source-id";
    protected const string SubjectValue = "explicit-subject";

    protected ISink _sink;
    protected IObjectComparer _objectComparer;
    protected IJsonComplianceManager _complianceManager;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected ReadModelDefinition _readModelDefinition;
    protected ReducerPipeline _pipeline;
    protected JsonSchema _schema;

    void Establish()
    {
        _sink = Substitute.For<ISink>();
        _objectComparer = Substitute.For<IObjectComparer>();
        _complianceManager = Substitute.For<IJsonComplianceManager>();
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();

        _schema = new JsonSchema();

        _readModelDefinition = new ReadModelDefinition(
            "test-read-model",
            "TestCollection",
            "Test Read Model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Reducer,
            "test-observer",
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, _schema } },
            []);

        _sink.FindOrDefault(Arg.Any<Key>()).Returns(Task.FromResult<ExpandoObject?>(null));

        _objectComparer.Compare(Arg.Any<ExpandoObject>(), Arg.Any<ExpandoObject>(), out Arg.Any<IEnumerable<PropertyDifference>>())
            .Returns(false);

        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(_ => new JsonObject());

        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>())
            .Returns(_ => new ExpandoObject());

        _complianceManager.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(ci => Task.FromResult(new JsonObject()));

        _complianceManager.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(ci => Task.FromResult(new JsonObject()));

        _sink.ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>())
            .Returns(Task.FromResult(Enumerable.Empty<FailedPartition>()));

        _pipeline = new ReducerPipeline(
            _readModelDefinition,
            _sink,
            _objectComparer,
            _complianceManager,
            _expandoObjectConverter,
            EventStore,
            EventStoreNamespace);
    }

    protected AppendedEvent CreateEvent(string eventSourceId, Subject? subject = null)
    {
        var context = EventContext.From(
            EventStore,
            EventStoreNamespace,
            EventType.Unknown,
            EventSourceType.Default,
            eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            CorrelationId.NotSet,
            subject: subject);
        return new AppendedEvent(context, new ExpandoObject());
    }

    protected ReducerContext CreateContext(string eventSourceId, Subject? subject = null) =>
        new(new[] { CreateEvent(eventSourceId, subject) }, new Key(eventSourceId, new ArrayIndexers([])));

    protected ReducerDelegate CreateReducer(ExpandoObject? returnState) =>
        (_, _) => Task.FromResult(new ReducerSubscriberResult(
            new ObserverSubscriberResult(ObserverSubscriberState.Ok, EventSequenceNumber.First, [], string.Empty),
            returnState));
}
