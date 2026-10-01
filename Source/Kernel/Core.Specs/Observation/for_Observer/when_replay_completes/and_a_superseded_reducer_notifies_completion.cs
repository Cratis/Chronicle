// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_completes;

public class and_a_superseded_reducer_notifies_completion : given.an_observer_with_subscription
{
    readonly JobId _replacement = JobId.New();
    async Task Establish()
    {
        _jobsManager.Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>()).Returns(Result<JobId, StartJobError>.Success(_replacement));
        await _observer.Replay();
    }
    async Task Because() => await _observer.ReplayedFor(JobId.New(), 42UL, new Dictionary<Concepts.Keys.Key, EventSequenceNumber>(), [], DateTimeOffset.UtcNow, false);
    [Fact] void should_keep_the_replacement_replaying() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);
    [Fact] void should_not_accept_the_old_jobs_watermark() => (_stateStorage.State.LastHandledEventSequenceNumber == (EventSequenceNumber)42UL).ShouldBeFalse();
}
