// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Pipelines;

namespace Aksio.Cratis.Events.Projections.for_Projections.given
{
    public class all_dependencies : Specification
    {
        protected Mock<IProjectionDefinitions> projection_definitions;
        protected Mock<IProjectionPipelineDefinitions> pipeline_definitions;
        protected Mock<IProjectionFactory> projection_factory;
        protected Mock<IProjectionPipelineFactory> pipeline_factory;

        void Establish()
        {
            projection_definitions = new();
            pipeline_definitions = new();
            projection_factory = new();
            pipeline_factory = new();
        }
    }
}
