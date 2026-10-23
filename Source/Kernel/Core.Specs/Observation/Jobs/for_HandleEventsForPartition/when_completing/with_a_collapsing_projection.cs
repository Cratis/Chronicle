// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_completing;

public class with_a_collapsing_projection : given.a_performing_job_step
{
    ICollapsingProjectionObserverSubscriber _subscriber;

    void Establish()
    {
        _subscriber = Substitute.For<ICollapsingProjectionObserverSubscriber>();
        _jobStep.SetupForTesting(_observer, _subscriber, "some-partition");
        _eventCursor.MoveNext().Returns(Task.FromResult(false));
    }

    Task Because() => _jobStep.InvokePerformStep(_performState);

    [Fact] async Task should_release_the_shared_activation() => await _subscriber.Received(1).ReleaseAfterPartition();
}
