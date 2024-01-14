// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Projections.Pipelines;
using Aksio.Cratis.Kernel.Projections;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Projections.for_ProjectionManager.given;

public class a_projection_manager_without_any_projections : Specification
{
    protected const string event_store_name = "event_store";
    protected const string event_store_namespace = "event_store_namespace";

    protected ProjectionManager manager;
    protected Mock<IExecutionContextManager> execution_context_manager;
    protected Mock<IProjectionFactory> projection_factory;
    protected Mock<IProjectionPipelineFactory> projection_pipeline_factory;

    void Establish()
    {
        execution_context_manager = new();
        execution_context_manager.Setup(_ => _.Current).Returns(new ExecutionContext(
            MicroserviceId.Unspecified,
            TenantId.Development,
            CorrelationId.New()));
        projection_factory = new();
        projection_pipeline_factory = new();
        manager = new ProjectionManager(
            event_store_name,
            event_store_namespace,
            projection_factory.Object,
            projection_pipeline_factory.Object,
            Mock.Of<ILogger<ProjectionManager>>());
    }
}
