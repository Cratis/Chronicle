// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Pipelines.JobSteps.for_Catchup.given;

public class all_dependencies : Specification
{
    protected static ProjectionResultStoreConfigurationId configuration = "8a1e35ac-567c-4309-957d-61910eb0c581";
    protected Mock<IProjectionPipeline> pipeline;
    protected Mock<IProjectionPositions> positions;
    protected Mock<IProjectionEventProvider> provider;
    protected Mock<IProjectionPipelineHandler> handler;
    protected ProjectionPipelineJobStatus job_status;
    protected Mock<IProjection> projection;
    protected Mock<IProjectionResultStore> result_store;

    void Establish()
    {
        pipeline = new();
        positions = new();
        provider = new();
        handler = new();
        job_status = new();

        projection = new();
        projection.SetupGet(_ => _.Identifier).Returns("12686cc9-3e6f-4161-80ac-2e5a1cbfb509");
        pipeline.SetupGet(_ => _.Projection).Returns(projection.Object);

        result_store = new();
        pipeline.SetupGet(_ => _.ResultStores)
                .Returns(new Dictionary<ProjectionResultStoreConfigurationId, IProjectionResultStore>
                {
                        { configuration, result_store.Object }
                });
    }
}
