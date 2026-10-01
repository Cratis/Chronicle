// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerReplay.when_abandoning;

public class and_a_replacement_has_started : given.a_reducer_replay
{
    ReplayContext _replacement;
    ReplayPublication _publication;
    async Task Establish() => _replacement = await _replay.Begin(JobId.New());
    async Task Because()
    {
        await _replay.Abandon(_jobId);
        _publication = await _replay.Publish(_context);
    }
    [Fact] void should_preserve_the_replacement_context() => _current!.RevertContainerName.ShouldEqual(_replacement.RevertContainerName);
    [Fact] void should_fence_late_publication_from_the_removed_job() => _publication.ShouldEqual(ReplayPublication.Superseded);
    [Fact] void should_never_swap_the_removed_jobs_partial_model() => _live.DidNotReceive().PublishReplay(Arg.Any<ReplayContext>(), Arg.Any<ISink>());
    [Fact] void should_not_change_any_silos_live_sink_mode() => _live.DidNotReceive().LeaveReplay();
}
