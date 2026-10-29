// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
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
using Cratis.Chronicle.Storage.InMemory.Sinks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating.given;

public class a_root_join : Specification
{
    protected static readonly Guid _rootId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    protected static readonly EventType _joinedEventType = new("Joined", EventTypeGeneration.First);
    protected IProjection _projection;
    protected IEventSequenceStorage _eventSequenceStorage;
    protected InMemorySink _sink;
    protected AppendedEvent _event;
    protected ResolvedKey _resolvedKey;
    protected ILogger<ProjectionFactory> _logger;
    protected virtual PropertyExpression JoinKey => "lookup.id";
    protected virtual string JoinValue => "00000000-0000-0000-0000-000000000011";
    protected virtual string JoinSource => "unrelated-event-source";
    protected IDictionary<string, object?> Root => _sink.Collection[_rootId];
    protected int WarningCount => _logger.ReceivedCalls().Count(call =>
        call.GetMethodInfo().Name == nameof(ILogger.Log) && (LogLevel)call.GetArguments()[0]! == LogLevel.Warning);

    async Task Establish()
    {
        EventStoreName eventStore = "event-store";
        EventStoreNamespaceName @namespace = "namespace";
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(EventSequenceId.Log).Returns(_eventSequenceStorage);
        var formats = new TypeFormats();
        var keys = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        _logger = Substitute.For<ILogger<ProjectionFactory>>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        var factory = new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            values,
            new KeyExpressionResolvers(values, keys, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(formats),
            keys,
            storage,
            _logger);
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string","format":"guid"},"joinValue":{"type":"string"},"name":{"type":"string"}}}
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
        var definition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "test-projection",
            "test-model",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>
            {
                [_joinedEventType] = new("joinValue", new Dictionary<PropertyPath, string> { ["name"] = "name" }, JoinKey),
                [new EventType("OtherJoined", EventTypeGeneration.First)] = new("joinValue", new Dictionary<PropertyPath, string> { ["name"] = "name" }, JoinKey)
            },
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled);
        _projection = await factory.Create(eventStore, @namespace, definition, readModel, []);
        _sink = new InMemorySink(readModel, formats);
        _sink.Collection[_rootId] = new { id = _rootId, joinValue = JoinValue, name = "before" }.AsExpandoObject();
        dynamic content = new ExpandoObject();
        content.lookup = new ExpandoObject();
        content.lookup.id = JoinValue;
        content.name = "after";
        _event = new AppendedEvent(
            EventContext.EmptyWithEventSourceId(JoinSource) with { EventType = _joinedEventType, SequenceNumber = 1 },
            (ExpandoObject)content);
    }

    protected async Task ApplyJoin()
    {
        _resolvedKey = (ResolvedKey)await _projection.GetKeyResolverFor(_joinedEventType)(_eventSequenceStorage, _sink, _event);
        var state = await _sink.FindOrDefault(_resolvedKey.Key) ?? new ExpandoObject();
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), _event, state);
        _projection.OnNext(new ProjectionEventContext(_resolvedKey.Key, _event, changeset, ProjectionOperationType.Join, false, _resolvedKey.JoinKey));
        var failures = await _sink.ApplyChanges(_resolvedKey.Key, changeset, _event.Context.SequenceNumber);
        failures.ShouldBeEmpty();
    }

    void Destroy() => _sink.Dispose();
}
