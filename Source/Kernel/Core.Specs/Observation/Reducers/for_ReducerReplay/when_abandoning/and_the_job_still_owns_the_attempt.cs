// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerReplay.when_abandoning;

public class and_the_job_still_owns_the_attempt : given.a_reducer_replay
{
    async Task Because() => await _replay.Abandon(_jobId);
    [Fact] void should_revoke_publication() => _current.ShouldBeNull();
    [Fact] void should_leave_the_published_model_untouched() => _live.DidNotReceive().PublishReplay(Arg.Any<ReplayContext>(), Arg.Any<ISink>());
    [Fact] void should_not_redirect_in_flight_replies_into_the_live_sink() => _live.DidNotReceive().LeaveReplay();
}
