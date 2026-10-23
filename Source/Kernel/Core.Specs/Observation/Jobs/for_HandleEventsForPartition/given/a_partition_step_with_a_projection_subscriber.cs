// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.given;

public class a_partition_step_with_a_projection_subscriber : a_performing_job_step
{
    protected IProjectionObserverSubscriber _projectionSubscriber;

    void Establish()
    {
        _projectionSubscriber = Substitute.For<IProjectionObserverSubscriber>();
        _jobStep.SetupForTesting(_observer, _projectionSubscriber, "some-partition");
        _eventCursor.MoveNext().Returns(Task.FromResult(false));
    }
}
