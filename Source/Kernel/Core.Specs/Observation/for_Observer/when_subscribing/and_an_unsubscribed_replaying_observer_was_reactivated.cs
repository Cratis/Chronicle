// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_an_unsubscribed_replaying_observer_was_reactivated : given.an_unsubscribed_replaying_observer
{
    JobId _jobId;

    async Task Establish()
    {
        await Reactivate();
        _jobId = JobId.New();
        _jobsManager.Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_jobId)));
    }

    Task Because() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

    [Fact] async Task should_be_replaying() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Replay>();
    [Fact] void should_keep_the_replay_flag() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] async Task should_report_the_started_replay_job() => ((Replay)await _observer.GetCurrentState()).LastStartedJobId.ShouldEqual(_jobId);
    [Fact] void should_start_one_replay_for_the_subscribed_event_types() => _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Is<ReplayObserverRequest>(_ => _.EventTypes.SequenceEqual(new[] { EventType.Unknown })));
    [Fact] void should_not_start_a_replay_with_empty_event_types() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Is<ReplayObserverRequest>(_ => !_.EventTypes.Any()));
}
