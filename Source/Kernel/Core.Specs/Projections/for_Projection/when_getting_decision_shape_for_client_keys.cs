// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_Projection;

public class when_getting_decision_shape_for_client_keys
{
    static readonly EventType _created = new("Created", EventTypeGeneration.First);
    static readonly EventType _removed = new("Removed", EventTypeGeneration.First);

    [Theory]
    [InlineData(0)] // Kernel-language implicit key
    [InlineData(1)] // Declarative .From<T>() and .RemovedWith<T>() builders
    [InlineData(2)] // Model-bound [FromEvent<T>] and [RemovedWith<T>]
    public async Task should_admit_client_from_and_removal_defaults(int shapeKind)
    {
        var key = shapeKind == 0 ? new PropertyExpression(string.Empty) : (PropertyExpression)WellKnownExpressions.EventSourceId;
        var definition = CreateDefinition(key, shapeKind == 1 ? new PropertyExpression(string.Empty) : null);
        if (shapeKind == 2)
        {
            definition.RemovedWith[_removed] = new(key, WellKnownExpressions.EventSourceId);
        }
        var grain = await CreateGrain(definition);
        var shape = await grain.GetDecisionProjectionShape(EventStoreNamespaceName.Default);
        shape.IsEventSourceKeyed.ShouldBeTrue();
        shape.EventTypes.ShouldContain(_created);
        shape.EventTypes.ShouldContain(_removed);
    }

    [Theory]
    [InlineData("$composite(id=$eventSourceId)", null)]
    [InlineData("$name", null)]
    [InlineData("fixed-key", null)]
    [InlineData("$eventSourceId", "parentId")]
    public async Task should_refuse_non_direct_from_or_parent_keys(string fromKey, string? parentKey)
    {
        var definition = CreateDefinition(fromKey, parentKey is null ? null : new PropertyExpression(parentKey));
        definition.RemovedWith[_removed] = new(WellKnownExpressions.EventSourceId, null);
        var grain = await CreateGrain(definition);
        (await grain.GetDecisionProjectionShape(EventStoreNamespaceName.Default)).IsEventSourceKeyed.ShouldBeFalse();
    }

    [Fact]
    public async Task should_refuse_a_custom_removal_key()
    {
        var definition = CreateDefinition(WellKnownExpressions.EventSourceId);
        definition.RemovedWith[_removed] = new("$name", null);
        var grain = await CreateGrain(definition);
        (await grain.GetDecisionProjectionShape(EventStoreNamespaceName.Default)).IsEventSourceKeyed.ShouldBeFalse();
    }

    static ProjectionDefinition CreateDefinition(PropertyExpression key, PropertyExpression? parentKey = null) => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        "projection",
        "model",
        true,
        true,
        new JsonObject(),
        new Dictionary<EventType, FromDefinition>
        {
            [_created] = new(new Dictionary<PropertyPath, string>(), key, parentKey)
        },
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition> { [_removed] = new(key, parentKey) },
        new Dictionary<EventType, RemovedWithJoinDefinition>());

    static async Task<Projection> CreateGrain(ProjectionDefinition definition)
    {
        var silo = new TestKitSilo();
        var storage = Substitute.For<Cratis.Chronicle.Storage.IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.EventTypes.GetLatestForAllEventTypes().Returns([]);
        var readModel = Substitute.For<IReadModel>();
        readModel.GetDefinition().Returns(new ReadModelDefinition(
            "model",
            "model",
            "Model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "projection",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new JsonSchema() },
            []));
        silo.AddProbe(_ => readModel);
        var formats = new TypeFormats();
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        silo.AddService<IProjectionFactory>(new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            values,
            new KeyExpressionResolvers(values, keyResolvers, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(formats),
            keyResolvers,
            storage,
            NullLogger<ProjectionFactory>.Instance));
        silo.AddService(storage);
        silo.AddService(Substitute.For<IProjectionDefinitionComparer>());
        silo.AddService(Substitute.For<Cratis.Chronicle.Changes.IObjectComparer>());
        silo.AddService(Options.Create(new ChronicleOptions()));
        silo.AddService(NullLogger<Projection>.Instance);
        var state = Substitute.For<IStorage<ProjectionDefinition>>();
        state.State = definition;
        silo.Options.StorageFactory = _ => state;
        return await silo.CreateGrainAsync<Projection>(new ProjectionKey("projection", "store").ToString());
    }
}
