// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_resuming_replay;

public class and_context_is_missing : given.a_reducer_replay_handler
{
    void Establish() => _contexts.TryGet(_context.Type.Identifier).Returns(GetContextError.NotFound);
    async Task Because() => _result = await _handler.ResumeReplayFor(_details);

    [Fact] void should_not_resume_sink() => _sink.DidNotReceiveWithAnyArgs().ResumeReplay(default!);
    [Fact] void should_report_error() => _result.TryGetError(out _).ShouldBeTrue();
}
