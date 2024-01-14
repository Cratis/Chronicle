// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.Compliance;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Projections;
using Aksio.Cratis.Kernel.Projections.Expressions;
using Aksio.Cratis.Kernel.Projections.Expressions.EventValues;
using Aksio.Cratis.Kernel.Projections.Expressions.Keys;
using Aksio.Cratis.Kernel.Projections.Pipelines;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.Changes;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Kernel.Storage.Sinks;
using Aksio.Cratis.Kernel.Storage.Sinks.InMemory;
using Aksio.Cratis.Models;
using Aksio.Cratis.Schemas;
using Aksio.Json;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Projections.for_ProjectionScenarios.given;

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
