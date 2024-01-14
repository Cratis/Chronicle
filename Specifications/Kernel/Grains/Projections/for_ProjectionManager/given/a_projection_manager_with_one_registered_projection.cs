// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Kernel.Projections.Pipelines;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Properties;
using EngineProjection = Aksio.Cratis.Kernel.Projections.IProjection;

namespace Aksio.Cratis.Kernel.Grains.Projections.for_ProjectionManager.given;

public class a_projection_manager_with_one_registered_projection : a_projection_manager_without_any_projections
{
    protected ProjectionDefinition projection_definition;
    protected ProjectionPipelineDefinition pipeline_definition;
    protected Mock<EngineProjection> projection;
    protected Mock<IProjectionPipeline> projection_pipeline;

    async Task Establish()
    {
        projection_definition = new ProjectionDefinition(
            Guid.NewGuid(),
            "SomeProjection",
            new ModelDefinition("Some Model", "{}"),
            false,
            false,
            new JsonObject(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            Enumerable.Empty<FromAnyDefinition>(),
            new AllDefinition(new Dictionary<PropertyPath, string>(), false));

        pipeline_definition = new ProjectionPipelineDefinition(projection_definition.Identifier, Enumerable.Empty<ProjectionSinkDefinition>());

        projection = new();
        projection_pipeline = new();

        projection_factory.Setup(_ => _.CreateFrom(projection_definition)).Returns(Task.FromResult(projection.Object));
        projection_pipeline_factory.Setup(_ => _.CreateFrom(projection.Object, pipeline_definition)).Returns(projection_pipeline.Object);

        await manager.Register(projection_definition, pipeline_definition);
    }
}
