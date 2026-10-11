// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned.given;

public class a_projection_with_generations : Specification
{
    protected EventType _first = new("PersonRegistered", 1);
    protected EventType _second = new("PersonRenamed", 2);
    protected ProjectionFactory _factory;
    protected ProjectionDefinition _definition;
    protected ReadModelDefinition _readModel;
    protected IEventSequenceStorage _sequence;
    protected EventTypeSchema[] _schemas;
    protected IEventGenerationRelease _release;
    protected IProjection _projection;
    protected ExpandoObject _state;

    async Task Establish()
    {
        var storage = Substitute.For<IStorage>();
        _sequence = Substitute.For<IEventSequenceStorage>();
        storage.GetEventStore("store").GetNamespace("namespace").GetEventSequence(EventSequenceId.Log).Returns(_sequence);
        _release = Substitute.For<IEventGenerationRelease>();
        var formats = new TypeFormats();
        var keys = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        _factory = new(
            new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            values,
            new KeyExpressionResolvers(values, keys, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(formats),
            keys,
            storage,
            NullLogger<ProjectionFactory>.Instance,
            eventGenerationRelease: _release);
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"oldName":{"type":"string"},"newName":{"type":"string"},"joinId":{"type":"string"},"items":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"oldName":{"type":"string"},"newName":{"type":"string"}}}}}}
            """);
        _readModel = new("model", "model", "model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []);
        var eventSchema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"name":{"type":"string"},"key":{"type":"string"},"parent":{"type":"string"}}}
            """);
        _schemas = [new(_first, EventTypeOwner.Client, EventTypeSource.Code, eventSchema), new(_second, EventTypeOwner.Client, EventTypeSource.Code, eventSchema)];
        _definition = new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "projection",
            "model",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition>
            {
                [_first] = new(new Dictionary<PropertyPath, string> { [(PropertyPath)"oldName"] = "name" }, "key", null),
                [_second] = new(new Dictionary<PropertyPath, string> { [(PropertyPath)"newName"] = "name" }, "key", null)
            },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled)
        { GenerationDelivery = EventGenerationDelivery.Pinned };
        _state = new();
    }

    protected AppendedEvent Event(EventType type, string name)
    {
        dynamic content = new ExpandoObject();
        content.name = name;
        content.key = $"key-{type.Generation.Value}";
        content.parent = "parent";
        return new(new EventContext(type, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 0, DateTimeOffset.UtcNow, "store", "namespace", CorrelationId.New(), [], Identity.System, [], EventHash.NotSet), (ExpandoObject)content);
    }

    protected void Project(IProjection projection, AppendedEvent incoming, ArrayIndexers? indexers = null)
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, _state);
        projection.OnNext(new(new Key("root", indexers ?? ArrayIndexers.NoIndexers), incoming, changeset, ProjectionOperationType.From, false));
        _state = changeset.CurrentState;
    }

    void Destroy() => (_projection as IDisposable)?.Dispose();
}
