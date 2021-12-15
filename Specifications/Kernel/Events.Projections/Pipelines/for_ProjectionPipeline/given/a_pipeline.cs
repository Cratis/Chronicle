// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections.Pipelines.for_ProjectionPipeline.given
{
    public class a_pipeline : all_dependencies
    {
        protected ProjectionPipeline pipeline;
        protected List<ProjectionState> states;

        void Establish()
        {
            states = new();
            pipeline = new(
                projection.Object,
                event_provider.Object,
                pipeline_handler.Object,
                jobs.Object,
                Mock.Of<ILogger<ProjectionPipeline>>());
            pipeline.State.Subscribe(_ => states.Add(_));
        }
    }
}
