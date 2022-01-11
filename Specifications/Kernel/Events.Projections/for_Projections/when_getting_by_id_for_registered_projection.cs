// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections.Pipelines;

namespace Cratis.Events.Projections.for_Projections
{
    public class when_getting_by_id_for_registered_projection : given.no_projections
    {
        IProjectionPipeline result;

        async Task Establish() => await projections.Register(projection_definition, pipeline_definition);

        void Because() => result = projections.GetById(projection_identifier);

        [Fact] void should_get_projection() => result.ShouldEqual(pipeline.Object);
    }
}
