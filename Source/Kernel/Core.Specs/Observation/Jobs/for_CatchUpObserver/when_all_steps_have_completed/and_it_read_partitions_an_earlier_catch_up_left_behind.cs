// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_all_steps_have_completed;

/// <summary>
/// An earlier catch-up got every partition to 12 but left events behind for one of them, and this catch-up read that
/// one only up to 9 - its last event below 12. Reporting 9 back would hold the observer's position below 12, and the
/// next catch-up would deliver the other partitions' events from 10 to 12 a second time. It reports 12 (#4583).
/// </summary>
public class and_it_read_partitions_an_earlier_catch_up_left_behind : given.a_catch_up_observer_job
{
    static readonly EventSequenceNumber _caughtUpTo = 12UL;

    void Establish() =>
        _request = _request with
        {
            ObserverType = ObserverType.Reactor,
            PartitionsLeftBehind = [new("partition", 8UL)],
            ToEventSequenceNumber = _caughtUpTo
        };

    async Task Because()
    {
        await _job.Start(_request);
        _stateStorage.State.LastHandledEventSequenceNumber = 9UL;
        await _job.CompleteAllSteps();
    }

    [Fact] async Task should_report_having_caught_up_to_where_the_earlier_catch_up_got() => await _observer.Received(1).CaughtUp(Arg.Any<JobId>(), _caughtUpTo);
}
