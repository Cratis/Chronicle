// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
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
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_subscribing_to_all_events_with_a_dynamic_dictionary_key : Specification
{
    static readonly EventStoreName _eventStore = "event-store";
    static readonly EventStoreNamespaceName _namespace = "namespace";
    static readonly EventType _activityLogged = new("d1b6c1a2-3d3a-4e2c-9f4a-1a2b3c4d5e6f", 1);
    static readonly EventType _userRegistered = new("f6e5d4c3-b2a1-4f3e-8d7c-6b5a4c3d2e1f", 1);
    static readonly EventSourceId _eventSourceId = "2f005aaf-2f4e-4a47-92ea-63687ef74bd4";

    IProjection _projection;
    Changeset<AppendedEvent, ExpandoObject> _changeset;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        var eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        storage.GetEventStore(_eventStore).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(_namespace).Returns(namespaceStorage);
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(eventSequenceStorage);

        var typeFormats = new TypeFormats();
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var eventValueProviderExpressionResolvers = new EventValueProviderExpressionResolvers(
            typeFormats,
            NullLogger<EventValueProviderExpressionResolvers>.Instance);

        var factory = new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(
                eventValueProviderExpressionResolvers,
                typeFormats,
                NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            eventValueProviderExpressionResolvers,
            new KeyExpressionResolvers(
                eventValueProviderExpressionResolvers,
                keyResolvers,
                NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(typeFormats),
            keyResolvers,
            storage,
            NullLogger<ProjectionFactory>.Instance);

        _projection = factory.Create(
            _eventStore,
            _namespace,
            CreateProjectionDefinition(),
            CreateReadModelDefinition(),
            []).GetAwaiter().GetResult();

        _changeset = new Changeset<AppendedEvent, ExpandoObject>(Substitute.For<IObjectComparer>(), EventOfType(_activityLogged), new ExpandoObject());
    }

    void Because()
    {
        var key = new Key(_eventSourceId.Value, ArrayIndexers.NoIndexers);

        _projection.OnNext(new ProjectionEventContext(key, EventOfType(_activityLogged), _changeset, ProjectionOperationType.From, false));

        _changeset.Incoming = EventOfType(_activityLogged);
        _projection.OnNext(new ProjectionEventContext(key, EventOfType(_activityLogged), _changeset, ProjectionOperationType.From, false));

        _changeset.Incoming = EventOfType(_userRegistered);
        _projection.OnNext(new ProjectionEventContext(key, EventOfType(_userRegistered), _changeset, ProjectionOperationType.From, false));
    }

    [Fact] void should_accept_event_types_it_was_never_explicitly_registered_for() =>
        _projection.Accepts(_activityLogged).ShouldBeTrue();

    [Fact] void should_be_event_source_keyed_by_default() => _projection.IsEventSourceKeyed.ShouldBeTrue();

    [Fact] void should_count_the_first_event_type_under_its_own_key() =>
        ((long)((IDictionary<string, object>)((dynamic)_changeset.CurrentState).eventCountByType)[_activityLogged.Id.Value]).ShouldEqual(2);

    [Fact] void should_count_the_second_event_type_under_its_own_key() =>
        ((long)((IDictionary<string, object>)((dynamic)_changeset.CurrentState).eventCountByType)[_userRegistered.Id.Value]).ShouldEqual(1);

    static AppendedEvent EventOfType(EventType eventType) => new(
        new(
            eventType,
            EventSourceType.Default,
            _eventSourceId,
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
        new ExpandoObject());

    static ProjectionDefinition CreateProjectionDefinition() => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        "Core.Admin.Events.EventCounts",
        "Core.Admin.Events.EventCounts",
        true,
        true,
        new(),
        new Dictionary<EventType, FromDefinition>(),
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new FromEveryDefinition(
            new Dictionary<PropertyPath, string>
            {
                [new PropertyPath("eventCountByType.$eventContext.eventType.id")] = WellKnownExpressions.Count
            },
            true),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>(),
        AutoMap: AutoMap.Disabled,
        SubscribesToAllEvents: true);

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "Core.Admin.Events.EventCounts",
            "eventCounts",
            "EventCounts",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = JsonSchema.FromJson("""
                    {
                      "type": "object",
                      "properties": {
                        "id": { "type": "string" },
                        "eventCountByType": { "type": "object" }
                      }
                    }
                    """)
            },
            []);
}
