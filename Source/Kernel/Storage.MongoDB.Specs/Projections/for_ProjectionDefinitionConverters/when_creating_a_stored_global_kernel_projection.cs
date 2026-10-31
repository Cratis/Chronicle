// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Statistics;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using static Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinitionConverters;
using StoredDefinition = Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionConverters;

public class when_creating_a_stored_global_kernel_projection : Specification
{
    static readonly EventStoreName _eventStore = "store";
    static readonly EventStoreNamespaceName _namespace = "tenant";
    IProjection _projection;
    IStorage _storage;
    ReadModelDefinition _readModel;

    async Task Because()
    {
        var declaration = KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection));
        _readModel = declaration.ReadModel;
        var namespaced = declaration.Projections.Single();
        var definition = namespaced with { Identifier = $"{namespaced.Identifier.Value}.global", Scope = ProjectionScope.Global };
        var restored = BsonSerializer.Deserialize<StoredDefinition>(definition.ToMongoDB().ToBsonDocument()).ToKernel();
        _storage = Substitute.For<IStorage>();
        _storage.GetEventStore(_eventStore).GetNamespace(EventStoreNamespaceName.NotSet).Sinks.GetFor(_readModel).Returns(Substitute.For<ISink>());
        _storage.GetEventStore(_eventStore).GetNamespace(_namespace).Sinks.GetFor(_readModel).Returns(Substitute.For<ISink>());
        var formats = new TypeFormats();
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var eventValues = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var factory = new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(eventValues, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            eventValues,
            new KeyExpressionResolvers(eventValues, keyResolvers, NullLogger<KeyExpressionResolvers>.Instance),
            new Cratis.Chronicle.Json.ExpandoObjectConverter(formats),
            keyResolvers,
            _storage,
            NullLogger<ProjectionFactory>.Instance);
        _projection = await factory.Create(_eventStore, _namespace, restored, _readModel, []);
        _storage.GetEventStore(_eventStore).GetNamespace(EventStoreNamespaceName.NotSet).Sinks.ClearReceivedCalls();
        _storage.GetEventStore(_eventStore).GetNamespace(_namespace).Sinks.ClearReceivedCalls();
        var pipelines = new ProjectionPipelineManager(
            _storage,
            Substitute.For<IGrainFactory>(),
            new ObjectComparer(),
            formats,
            Substitute.For<IReadModelsCompliance>(),
            Options.Create(new ChronicleOptions()),
            NullLoggerFactory.Instance);
        await pipelines.GetFor(_eventStore, _namespace, _projection);
    }

    [Fact] void should_create_the_global_projection() => _projection.Scope.ShouldEqual(ProjectionScope.Global);
    [Fact] void should_not_group_statistics_by_event_source() => _projection.IsEventSourceKeyed.ShouldBeFalse();
    [Fact] async Task should_materialize_into_the_global_sink() =>
        await _storage.GetEventStore(_eventStore).GetNamespace(EventStoreNamespaceName.NotSet).Sinks.Received(1).GetFor(_readModel);
    [Fact] async Task should_not_materialize_into_the_observed_tenant_sink() =>
        await _storage.GetEventStore(_eventStore).GetNamespace(_namespace).Sinks.DidNotReceive().GetFor(_readModel);
}
