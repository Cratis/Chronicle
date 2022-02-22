// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Events.Projections.Pipelines.for_ProjectionPipeline.given
{
    public class all_dependencies : Specification
    {
        protected Mock<IProjection> projection;
        protected Mock<IProjectionEventProvider> event_provider;

        protected Mock<IProjectionPipelineHandler> pipeline_handler;
        protected Mock<IProjectionPipelineJobs> jobs;
        protected ISubject<AppendedEvent> subject;
        protected ISubject<IReadOnlyDictionary<ProjectionSinkConfigurationId, EventSequenceNumber>> positions_per_configuration;

        void Establish()
        {
            event_provider = new();
            projection = new();
            projection.SetupGet(_ => _.IsPassive).Returns(false);
            projection.SetupGet(_ => _.IsRewindable).Returns(true);
            pipeline_handler = new();
            positions_per_configuration = new Subject<IReadOnlyDictionary<ProjectionSinkConfigurationId, EventSequenceNumber>>();
            pipeline_handler.SetupGet(_ => _.Positions).Returns(positions_per_configuration);
            jobs = new();
            subject = new Subject<AppendedEvent>();
        }
    }
}
