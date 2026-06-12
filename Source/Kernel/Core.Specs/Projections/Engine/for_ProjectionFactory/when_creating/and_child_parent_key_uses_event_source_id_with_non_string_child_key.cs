// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_child_parent_key_uses_event_source_id_with_non_string_child_key : Specification
{
    static readonly EventStoreName _eventStore = "event-store";
    static readonly EventStoreNamespaceName _namespace = "namespace";
    static readonly EventSourceId _userId = "00000000-0001-0000-0000-000000000001";
    static readonly EventType _userAdded = new("UserAdded", EventTypeGeneration.First);
    static readonly EventType _userRoleGranted = new("UserRoleGranted", EventTypeGeneration.First);

    IEventSequenceStorage _eventSequenceStorage;
    IProjection _projection;
    Key _result;

    void Establish()
    {
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStore(_eventStore).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(_namespace).Returns(namespaceStorage);
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(_eventSequenceStorage);

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

        var parentEvent = new AppendedEvent(
            EventContext.EmptyWithEventSourceId(_userId) with
            {
                EventType = _userAdded,
                SequenceNumber = 1
            },
            new { name = "Ada Lovelace" }.AsExpandoObject());

        _eventSequenceStorage
            .TryGetLastInstanceOfAny(_userId, Arg.Is<IEnumerable<EventTypeId>>(_ => _.Contains(_userAdded.Id)))
            .Returns(new Option<AppendedEvent>(parentEvent));
    }

    async Task Because()
    {
        var roleGrantedEvent = new AppendedEvent(
            EventContext.EmptyWithEventSourceId(_userId) with
            {
                EventType = _userRoleGranted,
                SequenceNumber = 2
            },
            new { role = 3 }.AsExpandoObject());

        var keyResult = await _projection.GetKeyResolverFor(_userRoleGranted)(
            _eventSequenceStorage,
            NullSink.Instance,
            roleGrantedEvent);

        _result = (keyResult as ResolvedKey).Key;
    }

    [Fact] void should_resolve_the_parent_key_from_the_event_source_id() => _result.Value.ShouldEqual(_userId.Value);

    [Fact] void should_keep_the_child_key_as_the_non_string_identifier()
    {
        var arrayIndexer = _result.ArrayIndexers.All.Single();
        arrayIndexer.ArrayProperty.ShouldEqual(new PropertyPath("[roles]"));
        arrayIndexer.IdentifierProperty.ShouldEqual(new PropertyPath("role"));
        arrayIndexer.Identifier.ShouldEqual(3);
    }

    static ProjectionDefinition CreateProjectionDefinition()
    {
        var rootFrom = new Dictionary<EventType, FromDefinition>
        {
            [_userAdded] = new(
                new Dictionary<PropertyPath, string>(),
                WellKnownExpressions.EventSourceId,
                null)
        };

        var rolesFrom = new Dictionary<EventType, FromDefinition>
        {
            [_userRoleGranted] = new(
                new Dictionary<PropertyPath, string>(),
                "role",
                WellKnownExpressions.EventSourceId)
        };

        return new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "Core.Admin.Users.Listing.User",
            "Core.Admin.Users.Listing.User",
            true,
            true,
            new(),
            rootFrom,
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>
            {
                ["roles"] = new(
                    "role",
                    rolesFrom,
                    new Dictionary<EventType, JoinDefinition>(),
                    new Dictionary<PropertyPath, ChildrenDefinition>(),
                    new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
                    new Dictionary<EventType, RemovedWithDefinition>(),
                    new Dictionary<EventType, RemovedWithJoinDefinition>(),
                    AutoMap: AutoMap.Disabled)
            },
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled);
    }

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "Core.Admin.Users.Listing.User",
            "users",
            "User",
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
                        "name": { "type": "string" },
                        "roles": {
                          "type": "array",
                          "items": {
                            "type": "object",
                            "properties": {
                              "role": { "type": "integer" }
                            }
                          }
                        }
                      }
                    }
                    """)
            },
            []);
}
