// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Expressions;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Projections.Expressions.Keys;
using Cratis.Chronicle.Projections.Pipelines;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sinks.InMemory;
using Cratis.Json;
using Cratis.Models;
using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections.for_ProjectionScenarios.given;

public abstract class a_projection_pipeline_for<TModel> : Specification
{
    protected ProjectionPipeline pipeline;
    protected ITypeFormats type_formats;
    protected ModelPropertyExpressionResolvers expression_resolvers;
    protected Mock<IEventStoreNamespaceStorage> event_store_namespace_storage;
    protected Mock<IEventSequenceStorage> event_sequence_storage;
    protected ISink sink;

    async Task Establish()
    {
        event_sequence_storage = new();
        type_formats = new TypeFormats();
        var eventValueProviderExpressionResolvers = new EventValueProviderExpressionResolvers(type_formats);
        expression_resolvers = new ModelPropertyExpressionResolvers(eventValueProviderExpressionResolvers, type_formats);
        var expandoObjectConverter = new ExpandoObjectConverter(type_formats);

        event_store_namespace_storage = new();
        event_store_namespace_storage.Setup(_ => _.GetEventSequence(IsAny<EventSequenceId>())).Returns(event_sequence_storage.Object);

        var projectionFactory = new ProjectionFactory(
            expression_resolvers,
            eventValueProviderExpressionResolvers,
            new KeyExpressionResolvers(eventValueProviderExpressionResolvers),
            expandoObjectConverter,
            event_store_namespace_storage.Object);

        var complianceMetadataResolver = new Mock<IComplianceMetadataResolver>();
        var jsonSchemaGenerator = new JsonSchemaGenerator(complianceMetadataResolver.Object);
        var eventTypes = new Mock<IEventTypes>();

        var modelNameResolver = new ModelNameResolver(new DefaultModelNameConvention());
        var builder = new ProjectionBuilderFor<TModel>(
            Guid.NewGuid(),
            modelNameResolver,
            eventTypes.Object,
            jsonSchemaGenerator,
            Globals.JsonSerializerOptions);

        Define(builder);

        var projectionDefinition = builder.Build();
        var projection = await projectionFactory.CreateFrom(projectionDefinition);
        sink = new InMemorySink(Model, type_formats);

        pipeline = new ProjectionPipeline(
            projection,
            event_sequence_storage.Object,
            sink,
            new ObjectComparer(),
            new NullChangesetStorage(),
            type_formats,
            Mock.Of<ILogger<ProjectionPipeline>>());
    }

    protected abstract Model Model { get; }

    protected abstract void Define(IProjectionBuilderFor<TModel> builder);
}
