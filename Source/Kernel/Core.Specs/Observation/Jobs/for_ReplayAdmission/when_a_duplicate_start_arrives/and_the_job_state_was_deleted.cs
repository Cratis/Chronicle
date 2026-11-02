// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayAdmission.when_a_duplicate_start_arrives;

/// <summary>
/// The replay finished, its job state was removed and the observer released the claim. A start carrying the old epoch
/// that arrives late finds a job with no history to say it already ran, so only the claim can refuse it.
/// </summary>
public class and_the_job_state_was_deleted : given.an_admitting_replay_job
{
    Result<StartJobError> _result;

    void Establish() => _persistedObserverState = ObserverState.Empty;

    async Task Because()
    {
        _result = await _job.Start(RequestWithEpoch(1));
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_refuse_the_start() => (_result.TryGetError(out var error) && error == StartJobError.NotAdmitted).ShouldBeTrue();
    [Fact] void should_not_persist_the_job() => _stateStorage.State.Request.ShouldBeNull();
    [Fact] void should_not_call_the_observer() => _observer.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_notify_any_subscriber() => _replayServiceClient.ReceivedCalls().ShouldBeEmpty();
}
