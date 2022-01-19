// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Projections.Pipelines.JobSteps.for_Rewind.given
{
    public class a_rewind_step : Specification
    {
        protected static ProjectionResultStoreConfigurationId configuration = "8a1e35ac-567c-4309-957d-61910eb0c581";
        protected Mock<IProjectionPipeline> pipeline;
        protected Mock<IProjectionPositions> positions;
        protected Rewind rewind;
        protected ProjectionPipelineJobStatus job_status;
        protected Mock<IProjectionResultStore> result_store;
        protected Mock<IProjection> projection;
        protected Mock<IProjectionResultStoreRewindScope>   rewind_scope;

        void Establish()
        {
            pipeline = new();
            projection = new();
            projection.SetupGet(_ => _.Identifier).Returns("12686cc9-3e6f-4161-80ac-2e5a1cbfb509");
            pipeline.SetupGet(_ => _.Projection).Returns(projection.Object);

            result_store = new();
            pipeline.SetupGet(_ => _.ResultStores)
                    .Returns(new Dictionary<ProjectionResultStoreConfigurationId, IProjectionResultStore>
                    {
                        { configuration, result_store.Object }
                    });
            positions = new();
            rewind_scope = new();
            result_store.Setup(_ => _.BeginRewind()).Returns(rewind_scope.Object);
            rewind = new(pipeline.Object, positions.Object, configuration, Mock.Of<ILogger<Rewind>>());
            job_status = new();
        }
    }
}
