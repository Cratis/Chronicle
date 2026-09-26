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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_unsupported_model_bound_joins : Specification
{
    ILogger<ProjectionFactory> _logger;
    IProjection _root;
    IProjection _withChildren;

    async Task Because()
    {
        _logger = Substitute.For<ILogger<ProjectionFactory>>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        EventStoreName eventStore = "event-store";
        EventStoreNamespaceName @namespace = "namespace";
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(Substitute.For<IEventSequenceStorage>());
        eventStoreStorage.GetNamespace(@namespace).Returns(namespaceStorage);
        storage.GetEventStore(eventStore).Returns(eventStoreStorage);
        var formats = new TypeFormats();
        var keys = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var factory = new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            values,
            new KeyExpressionResolvers(values, keys, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(formats),
            keys,
            storage,
            _logger);
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"employees":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"details":{"type":"object","properties":{"reference":{"type":"string"},"value":{"type":"string"}}}}}}}}
            """);
        var readModel = new ReadModelDefinition(
            "test-model",
            "test-model",
            "test-model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []);
        var nested = new ChildrenDefinition(
            PropertyPath.NotSet,
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>
            {
                [(EventType)"NestedJoined"] = new((PropertyPath)"reference", new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet)
            },
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>
            {
                [(EventType)"NestedRemoved"] = new(PropertyExpression.NotSet)
            });
        var child = new ChildrenDefinition(
            (PropertyPath)"id",
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            Nested: new Dictionary<PropertyPath, ChildrenDefinition> { [(PropertyPath)"details"] = nested });
        var definition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "test-projection",
            "test-model",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition> { [(PropertyPath)"employees"] = child },
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition> { [(EventType)"RootRemoved"] = new(PropertyExpression.NotSet) },
            AutoMap: AutoMap.Disabled,
            Nested: new Dictionary<PropertyPath, ChildrenDefinition> { [(PropertyPath)"details"] = nested });
        _root = await factory.Create(eventStore, @namespace, definition, readModel, []);
        _withChildren = _root.ChildProjections.Single();
    }

    bool Warned(string eventName) => _logger.ReceivedCalls().Any(_ =>
        _.GetMethodInfo().Name == nameof(ILogger.Log) &&
        (LogLevel)_.GetArguments()[0]! == LogLevel.Warning &&
        ((EventId)_.GetArguments()[1]!).Name == eventName);

    [Fact] void should_not_subscribe_to_root_join_removal() => _root.EventTypes.ShouldNotContain((EventType)"RootRemoved");
    [Fact] void should_warn_about_root_join_removal() => Warned("RootRemovalViaJoinNotSupported").ShouldBeTrue();
    [Fact] void should_not_subscribe_to_nested_join_under_children() => _withChildren.EventTypes.ShouldNotContain((EventType)"NestedJoined");
    [Fact] void should_not_wire_nested_join_under_children() => ((Projection)_withChildren).Subscriptions.ShouldBeEmpty();
    [Fact] void should_warn_about_nested_join_under_children() => Warned("NestedJoinInChildrenNotSupported").ShouldBeTrue();
    [Fact] void should_warn_about_nested_join_removal() => Warned("NestedRemovalViaJoinNotSupported").ShouldBeTrue();
}
