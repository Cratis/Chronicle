// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.for_ProjectionPipeline
{
    public class when_pausing : given.a_pipeline
    {
        void Because() => pipeline.Pause();

        [Fact] void should_pause_event_provider() => event_provider.Verify(_ => _.Pause(projection.Object), Once());
    }
}
