// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_resuming_replay;

public class and_context_exists : given.a_reducer_replay_handler
{
    async Task Because() => _result = await _handler.ResumeReplayFor(_details);

    [Fact] void should_resume_the_sink() => _sink.Received(1).ResumeReplay(_context);
    [Fact]
    void should_disable_buffering_before_acknowledging_batches() => Received.InOrder(() =>
    {
        _sink.ResumeReplay(_context);
        _pipeline.EndBulk();
    });
    [Fact] void should_succeed() => _result.TryGetError(out _).ShouldBeFalse();
}
