// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States.for_Replay.when_entering_with_ownership_claims;

public class and_no_jobs_are_running : given.a_replay_state_with_ownership_claims
{
    ReplayObserverRequest _request;

    void Establish()
    {
        _claims.GrantObserverWideBeforeStart(ClaimHolderKind.ObserverReplay, EventSequenceNumber.First).Returns(9UL);
        _jobsManager
            .When(_ => _.Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>()))
            .Do(callInfo => _request = callInfo.Arg<ReplayObserverRequest>());
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_start_the_job_with_the_granted_epoch() => _request.OwnershipEpoch.ShouldEqual(9UL);
    [Fact] void should_grant_the_claim_before_starting_the_job() => Received.InOrder(() =>
    {
        _claims.GrantObserverWideBeforeStart(ClaimHolderKind.ObserverReplay, EventSequenceNumber.First);
        _jobsManager.Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    });
}
