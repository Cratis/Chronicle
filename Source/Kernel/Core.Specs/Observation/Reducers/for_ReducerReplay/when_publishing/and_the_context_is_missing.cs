// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerReplay.when_publishing;

public class and_the_context_is_missing : given.a_reducer_replay
{
    ReplayPublication _result;
    void Establish() => _current = null;
    async Task Because() => _result = await _replay.Publish(_context);
    [Fact] void should_refuse_unowned_publication() => _result.ShouldEqual(ReplayPublication.Superseded);
    [Fact] void should_not_touch_the_live_model() => _live.DidNotReceive().PublishReplay(Arg.Any<ReplayContext>(), Arg.Any<ISink>());
}
