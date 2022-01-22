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

        void Establish()
        {
            event_provider = new();
            projection = new();
            pipeline_handler = new();
            jobs = new();
            subject = new Subject<AppendedEvent>();
        }
    }
}
