// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Projections.Changes;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Expressions;
using Aksio.Cratis.Events.Projections.InMemory;
using Aksio.Cratis.Events.Projections.Pipelines;
using Aksio.Cratis.Events.Store;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Projections.for_ProjectionScenarios.given;

public abstract class a_projection_pipeline : Specification
{
    protected ProjectionPipeline pipeline;
    protected Mock<IEventSequenceStorageProvider> event_sequence_storage_provider;
    protected IProjectionSink sink;

    async Task Establish()
    {
        var projectionFactory = new ProjectionFactory(new PropertyMapperExpressionResolvers());
        var projection = await projectionFactory.CreateFrom(Projection);
        event_sequence_storage_provider = new();
        sink = new InMemoryProjectionSink();

        pipeline = new ProjectionPipeline(
            projection,
            event_sequence_storage_provider.Object,
            sink,
            new ObjectsComparer(),
            new NullChangesetStorage(),
            Mock.Of<ILogger<ProjectionPipeline>>());
    }

    protected abstract ProjectionDefinition Projection { get; }
}


public class when_projecting : given.a_projection_pipeline
{
    protected override ProjectionDefinition Projection => new(
        Guid.NewGuid(),
        "TestProjection",
        new ModelDefinition("", ""),
        true,
        new()
        {

        },
        new()
        {

        });
    )
}
