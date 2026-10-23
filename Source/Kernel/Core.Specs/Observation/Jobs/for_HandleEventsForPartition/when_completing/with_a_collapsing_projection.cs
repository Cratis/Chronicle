// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_completing;

public class with_a_collapsing_projection : given.a_performing_job_step
{
    ICollapsingProjectionObserverSubscriber _subscriber;

    void Establish()
    {
        _subscriber = Substitute.For<ICollapsingProjectionObserverSubscriber, IProjectionObserverSubscriber>();
        _jobStep.SetupForTesting(_observer, _subscriber, "some-partition");
        _eventCursor.MoveNext().Returns(Task.FromResult(false));
    }

    Task Because() => _jobStep.InvokePerformStep(_performState);

    [Fact] async Task should_keep_the_shared_activation_warm() => await ((IProjectionObserverSubscriber)_subscriber).DidNotReceive().ReleaseAfterPartition();
    [Fact] void should_not_expose_partition_release_on_the_shared_interface() => typeof(ICollapsingProjectionObserverSubscriber).GetMethods().ShouldNotContain(method => method.Name == nameof(IProjectionObserverSubscriber.ReleaseAfterPartition));
}
