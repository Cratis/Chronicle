// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_steps_could_not_be_prepared.given;

public class a_replaying_observer : for_Observer.given.an_observer_with_subscription
{
    async Task Establish()
    {
        _jobsManager
            .Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reducer, [EventType.Unknown], SiloAddress.Zero);
        _stateStorage.State = _stateStorage.State with
        {
            LastHandledEventSequenceNumber = 42,
            NextEventSequenceNumber = 43
        };
        await _observer.Replay();
        _jobsManager.ClearReceivedCalls();
        _storageStats.ResetCounts();
    }

    protected async Task RunWatchdogTicks()
    {
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
    }
}
