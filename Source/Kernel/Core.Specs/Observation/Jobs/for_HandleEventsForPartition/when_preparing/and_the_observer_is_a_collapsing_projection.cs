// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_preparing;

public class and_the_observer_is_a_collapsing_projection : given.a_prepared_job_step_for_subscriber_type
{
    protected override Type SubscriberType => typeof(ICollapsingProjectionObserverSubscriber);

    [Fact] void should_checkpoint_after_every_batch() => _jobStep.IsCheckpointingAfterEveryBatch.ShouldBeTrue();
}
