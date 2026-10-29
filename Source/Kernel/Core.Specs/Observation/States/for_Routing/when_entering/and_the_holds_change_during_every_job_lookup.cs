// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_the_holds_change_during_every_job_lookup : given.a_routing_state_with_partition_holds
{
    int _version;
    int _lookups;

    void Establish()
    {
        _holdsVersion = () => _version;
        _jobStorage
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(_ =>
            {
                _lookups++;
                _version++;
                return Task.FromResult(Catch<IImmutableList<JobState>>.Success(ImmutableList<JobState>.Empty));
            });
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_give_up_after_three_lookups() => _lookups.ShouldEqual(3);
    [Fact] void should_keep_the_catching_up_partition_held() => _resultingStoredState.CatchingUpPartitions.ShouldContain(_catchingUpPartition);
    [Fact] void should_keep_the_replaying_partition_held() => _resultingStoredState.ReplayingPartitions.ShouldContain(_replayingPartition);
}
