// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerReplay.when_beginning;

public class and_previous_publication_has_an_unknown_outcome : given.a_reducer_replay
{
    ReplayContext _next;
    void Establish() => _jobs.Read<JobStateWithLastHandledEvent>(_jobId).Returns(Catch<JobStateWithLastHandledEvent, Cratis.Orleans.Storage.Jobs.JobError>.Success(new JobStateWithLastHandledEvent
    {
        ReducerReplayPhase = ReducerReplayPhase.Publishing,
        LastHandledEventSequenceNumber = 42UL
    }));
    async Task Because() => _next = await _replay.Begin(JobId.New());
    [Fact]
    void should_reconcile_the_previous_swap_before_preparing_the_replacement() => Received.InOrder(() =>
    {
        _live.PublishReplay(_context, _shadow);
        _live.PrepareReplay(_next);
    });
    [Fact] void should_admit_the_replacement_only_after_reconciliation() => _current!.RevertContainerName.ShouldEqual(_next.RevertContainerName);
}
