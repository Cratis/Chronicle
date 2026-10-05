// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_resuming_replay;

public class and_the_context_exists : given.a_reducer_replay_handler
{
    Task Because() => _handler.ResumeReplayFor(_observerDetails);

    [Fact] void should_resume_the_sink_into_the_replay() => _sink.Received(1).ResumeReplay(_replayContext);
}
