// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_beginning_replay;

public class and_context_exists : given.a_reducer_replay_handler
{
    async Task Because() => _result = await _handler.BeginReplayFor(_details);

    [Fact] void should_begin_replay() => _pipeline.Received(1).BeginReplay(_context);
    [Fact]
    void should_disable_buffering_before_acknowledging_batches() => Received.InOrder(() =>
    {
        _pipeline.BeginReplay(_context);
        _pipeline.EndBulk();
        _mediator.OnBeginReplay(new("reducer"), _details.Key.EventStore, _details.Key.Namespace);
    });
    [Fact] void should_succeed() => _result.TryGetError(out _).ShouldBeFalse();
}
