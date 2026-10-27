// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

/// <summary>
/// Creates directly from a stored legacy definition, without a transport conversion.
/// </summary>
public class and_a_stored_from_key_is_the_bare_event_source_id : Specification
{
    static readonly EventType _eventType = new("event-type", 1);

    ProjectionFactory _factory;
    ProjectionDefinition _definition;
    ReadModelDefinition _readModel;
    EventTypeSchema _eventSchema;
    IProjection _projection;
    object _firstKey;
    object _secondKey;

    void Establish()
    {
        _definition = new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "legacy-keys",
            "legacy-model",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition> { [_eventType] = new(new Dictionary<PropertyPath, string>(), "EventSourceId", PropertyExpression.NotSet) },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled);
        _readModel = new(
            "legacy-model",
            "legacyModels",
            "Legacy models",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"}}}""")
            },
            []);
        _eventSchema = new EventTypeSchema(
            _eventType,
            EventTypeOwner.Client,
            EventTypeSource.User,
            JsonSchema.FromJson("""{"type":"object","properties":{"name":{"type":"string"}}}"""));
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceStorage);
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(Substitute.For<IEventSequenceStorage>());
        var typeFormats = new TypeFormats();
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var eventValues = new EventValueProviderExpressionResolvers(typeFormats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        _factory = new(
            new ReadModelPropertyExpressionResolvers(eventValues, typeFormats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            eventValues,
            new KeyExpressionResolvers(eventValues, keyResolvers, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(typeFormats),
            keyResolvers,
            storage,
            NullLogger<ProjectionFactory>.Instance);
    }

    async Task Because()
    {
        _projection = await _factory.Create(
            "event-store",
            "namespace",
            _definition,
            _readModel,
            [_eventSchema]);
        _firstKey = await ResolveKey("first-source");
        _secondKey = await ResolveKey("second-source");
    }

    [Fact] void should_use_the_first_event_source_id() => _firstKey.ShouldEqual("first-source");
    [Fact] void should_use_the_second_event_source_id() => _secondKey.ShouldEqual("second-source");
    [Fact] void should_resolve_distinct_keys() => _firstKey.ShouldNotEqual(_secondKey);
    [Fact] void should_leave_the_stored_key_unchanged() => _definition.From[_eventType].Key.Value.ShouldEqual("EventSourceId");
    [Fact] void should_fingerprint_the_stored_definition() => _projection.DefinitionFingerprint.ShouldEqual(ProjectionFactory.DefinitionFingerprintFor(_definition));

    async Task<object> ResolveKey(EventSourceId eventSourceId)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["name"] = "x";
        var @event = new AppendedEvent(
            new(
                _eventType,
                EventSourceType.Default,
                eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                0,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                EventHash.NotSet),
            content);
        var result = await _projection.GetKeyResolverFor(_eventType)(Substitute.For<IEventSequenceStorage>(), Substitute.For<ISink>(), @event);
        return ((ResolvedKey)result).Key.Value;
    }
}
