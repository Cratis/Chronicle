// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_ending_replay;

public class and_context_is_missing : given.a_reducer_replay_handler
{
    void Establish() => _contexts.TryGet(_context.Type.Identifier).Returns(GetContextError.NotFound);
    async Task Because() => _result = await _handler.EndReplayFor(_details);

    [Fact] void should_leave_the_cached_sink_replay_mode() => _sink.Received(1).LeaveReplay();
    [Fact] void should_not_promote() => _pipeline.DidNotReceiveWithAnyArgs().EndReplay(default!);
    [Fact] void should_report_missing_context() => _result.TryGetError(out _).ShouldBeTrue();
    [Fact] void should_notify_clients_of_end() => _mediator.Received(1).OnEndReplay(new("reducer"), _details.Key.EventStore, _details.Key.Namespace);
}
