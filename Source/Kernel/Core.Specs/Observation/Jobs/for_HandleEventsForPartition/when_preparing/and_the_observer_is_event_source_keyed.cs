// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_preparing;

public class and_the_observer_is_event_source_keyed : given.a_prepared_job_step_for_subscriber_type
{
    protected override Type SubscriberType => typeof(IProjectionObserverSubscriber);

    [Fact] void should_keep_the_configured_checkpoint_interval() => _jobStep.IsCheckpointingAfterEveryBatch.ShouldBeFalse();
}
