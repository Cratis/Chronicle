// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.for_ProjectionPipeline
{
    public class when_resuming : given.a_pipeline
    {
        void Because() => pipeline.Resume();

        [Fact] void should_resume_event_provider() => event_provider.Verify(_ => _.Resume(projection.Object), Once());
    }
}
