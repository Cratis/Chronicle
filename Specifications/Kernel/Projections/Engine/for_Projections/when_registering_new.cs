// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Pipelines;

namespace Aksio.Cratis.Events.Projections.for_Projections;

public class when_registering_new : given.no_projections
{
    IEnumerable<IProjectionPipeline> pipeline_registered;

    void Establish()
    {
        projection_definitions.Setup(_ => _.HasFor(projection_identifier)).Returns(Task.FromResult(false));
        projection_definitions.Setup(_ => _.HasChanged(projection_definition)).Returns(Task.FromResult(false));
        projections.Pipelines.Subscribe(_ => pipeline_registered = _);
    }

    async Task Because() => await projections.Register(projection_definition, pipeline_definition);

    [Fact] void should_never_rewind_pipeline() => pipeline.Verify(_ => _.Rewind(), Never());
    [Fact] void should_register_projection_definition() => projection_definitions.Verify(_ => _.Register(projection_definition), Once());
    [Fact] void should_register_pipeline_definition() => pipeline_definitions.Verify(_ => _.Register(pipeline_definition), Once());
    [Fact] void should_make_pipeline_the_next_in_observable() => pipeline_registered.ShouldEqual(pipeline.Object);
    [Fact] void should_register_pipeline() => projections.GetPipelines().First().ShouldEqual(pipeline.Object);
}
