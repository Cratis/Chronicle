// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_beginning_replay;

public class and_establish_fails : given.a_reducer_replay_handler
{
    void Establish() => _contexts.Establish(_context.Type, _context.ContainerName).ThrowsAsync(new Exception("establish failed"));
    async Task Because() => _result = await _handler.BeginReplayFor(_details);

    [Fact] void should_report_initialization_failure() => _result.TryGetError(out _).ShouldBeTrue();
    [Fact] void should_not_begin_replay() => _pipeline.DidNotReceiveWithAnyArgs().BeginReplay(default!);
    [Fact] void should_not_notify_clients_of_begin() => _mediator.DidNotReceiveWithAnyArgs().OnBeginReplay(default!, default!, default!);
    [Fact] void should_fail_initialization_validation() => Catch.Exception(() => ObserverService.EnsureReplayStarted([_result])).ShouldBeOfExactType<ReplayInitializationFailed>();
}
