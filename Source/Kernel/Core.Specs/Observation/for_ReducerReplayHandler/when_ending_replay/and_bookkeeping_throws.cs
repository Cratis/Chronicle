// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_ending_replay;

public class and_bookkeeping_throws : given.a_reducer_replay_handler
{
    void Establish() => _manager.Replayed(_details.Key.ObserverId, _context).ThrowsAsync(new Exception("bookkeeping failed"));
    async Task Because() => _result = await _handler.EndReplayFor(_details);

    [Fact] void should_report_error() => _result.TryGetError(out _).ShouldBeTrue();
    [Fact] void should_notify_clients_even_after_promotion() => _mediator.Received(1).OnEndReplay(new("reducer"), _details.Key.EventStore, _details.Key.Namespace);
}
