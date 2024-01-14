// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Properties;
using EngineProjection = Aksio.Cratis.Kernel.Projections.IProjection;

namespace Aksio.Cratis.Kernel.Grains.Projections.for_ProjectionManager;

public class when_registering : given.a_projection_manager_without_any_projections
{
    ProjectionDefinition projection_definition;
    ProjectionPipelineDefinition pipeline_definition;

    void Establish()
    {
        projection_definition = new ProjectionDefinition(
            "912980d9-9de3-424f-95f2-6ebce81d6007",
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
    }

    async Task Because() => await manager.Register(projection_definition, pipeline_definition);

    [Fact] void should_create_projection() => projection_factory.Verify(_ => _.CreateFrom(projection_definition), Once);
    [Fact] void should_create_pipeline() => projection_pipeline_factory.Verify(_ => _.CreateFrom(IsAny<EngineProjection>(), pipeline_definition), Once);
}
