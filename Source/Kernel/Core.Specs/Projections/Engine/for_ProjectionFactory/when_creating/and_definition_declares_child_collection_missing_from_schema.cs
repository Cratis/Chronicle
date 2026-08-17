// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
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

/// <summary>
/// The failure shape from https://github.com/Cratis/Chronicle/issues/3722: the stored projection definition still
/// declares a child collection the read model schema no longer has. Building the projection must fail with an
/// exception naming the projection, the child property and the read model - not a bare KeyNotFoundException.
/// </summary>
public class and_definition_declares_child_collection_missing_from_schema : Specification
{
    static readonly EventStoreName _eventStore = "event-store";
    static readonly EventStoreNamespaceName _namespace = "namespace";
    static readonly EventType _conversationStarted = new("ConversationStarted", EventTypeGeneration.First);
    static readonly EventType _reactionGiven = new("ReactionGiven", EventTypeGeneration.First);

    ProjectionFactory _factory;
    Exception _exception;

    void Establish()
    {
        var eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStore(_eventStore).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(_namespace).Returns(namespaceStorage);
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(eventSequenceStorage);

        var typeFormats = new TypeFormats();
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var eventValueProviderExpressionResolvers = new EventValueProviderExpressionResolvers(
            typeFormats,
            NullLogger<EventValueProviderExpressionResolvers>.Instance);
        _factory = new ProjectionFactory(
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
    }

    async Task Because() => _exception = await Catch.Exception(() => _factory.Create(
        _eventStore,
        _namespace,
        CreateProjectionDefinition(),
        CreateReadModelDefinition(),
        []));

    [Fact] void should_fail_with_a_descriptive_exception() => _exception.ShouldBeOfExactType<MissingChildCollectionInReadModelSchema>();
    [Fact] void should_name_the_projection() => _exception.Message.ShouldContain("Chat.ChatTopicConversation");
    [Fact] void should_name_the_child_property() => _exception.Message.ShouldContain("reactions");
    [Fact] void should_name_the_read_model() => _exception.Message.ShouldContain("Chat.ChatTopicConversationReadModel");

    static ProjectionDefinition CreateProjectionDefinition()
    {
        var rootFrom = new Dictionary<EventType, FromDefinition>
        {
            [_conversationStarted] = new(
                new Dictionary<PropertyPath, string>(),
                WellKnownExpressions.EventSourceId,
                null)
        };

        var reactionsFrom = new Dictionary<EventType, FromDefinition>
        {
            [_reactionGiven] = new(
                new Dictionary<PropertyPath, string>(),
                "reactionId",
                WellKnownExpressions.EventSourceId)
        };

        return new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "Chat.ChatTopicConversation",
            "Chat.ChatTopicConversationReadModel",
            true,
            true,
            new(),
            rootFrom,
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>
            {
                ["reactions"] = new(
                    "reactionId",
                    reactionsFrom,
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
            "Chat.ChatTopicConversationReadModel",
            "chatTopicConversations",
            "ChatTopicConversation",
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
                        "comments": {
                          "type": "array",
                          "items": {
                            "type": "object",
                            "properties": {
                              "commentId": { "type": "string" }
                            }
                          }
                        }
                      }
                    }
                    """)
            },
            []);
}
