// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States.for_Replay.when_entering_with_ownership_claims;

public class and_no_claim_could_be_persisted : given.a_replay_state_with_ownership_claims
{
    ReplayObserverRequest _request;

    void Establish()
    {
        _claims.GrantObserverWideBeforeStart(ClaimHolderKind.ObserverReplay, EventSequenceNumber.First).Returns((ulong?)null);
        _jobsManager
            .When(_ => _.Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>()))
            .Do(callInfo => _request = callInfo.Arg<ReplayObserverRequest>());
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_still_start_the_job() => _request.ShouldNotBeNull();
    [Fact] void should_start_it_unchecked() => _request.OwnershipEpoch.ShouldBeNull();
}
