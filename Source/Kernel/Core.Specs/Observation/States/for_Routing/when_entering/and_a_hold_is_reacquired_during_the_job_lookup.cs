// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_a_hold_is_reacquired_during_the_job_lookup : given.a_routing_state_with_partition_holds
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
                if (_lookups == 1) _version++;
                IImmutableList<JobState> jobs = _lookups == 1 ? [] : [PartitionCatchUpJob(_catchingUpPartition)];
                return Task.FromResult(Catch<IImmutableList<JobState>>.Success(jobs));
            });
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_look_up_the_jobs_again() => _lookups.ShouldEqual(2);
    [Fact] void should_keep_the_reacquired_hold() => _resultingStoredState.CatchingUpPartitions.ShouldContain(_catchingUpPartition);
    [Fact] void should_release_the_replaying_partition_without_a_job() => _resultingStoredState.ReplayingPartitions.ShouldBeEmpty();
}
