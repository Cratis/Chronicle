// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_completes;

public class and_the_current_reducer_published_an_empty_model : given.an_observer_with_subscription
{
    readonly JobId _job = JobId.New();
    async Task Establish()
    {
        _jobsManager.Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>()).Returns(Result<JobId, StartJobError>.Success(_job));
        await _observer.Replay();
    }
    async Task Because() => await _observer.ReplayedFor(_job, 42UL, new Dictionary<Concepts.Keys.Key, EventSequenceNumber>(), [], DateTimeOffset.UtcNow, false);
    [Fact] void should_continue_after_the_published_prefix_without_a_document_watermark() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_leave_replay() => _stateStorage.State.IsReplaying.ShouldBeFalse();
}
