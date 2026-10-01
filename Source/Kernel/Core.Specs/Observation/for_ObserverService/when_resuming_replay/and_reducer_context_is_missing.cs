// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ObserverService.when_resuming_replay;

public class and_reducer_context_is_missing : Specification
{
    ICanHandleReplayForObserver _reducerHandler;
    ICanHandleReplayForObserver _otherHandler;
    ObserverDetails _details;
    Exception _error;

    void Establish()
    {
        _details = new(new("reducer", "store", "namespace", "event-log"), ObserverType.Reducer);
        _reducerHandler = Substitute.For<ICanHandleReplayForObserver>();
        _otherHandler = Substitute.For<ICanHandleReplayForObserver>();
        _reducerHandler.ResumeReplayFor(_details).Returns(Result.Failed(ICanHandleReplayForObserver.Error.CouldNotGetReplayContext));
        _otherHandler.ResumeReplayFor(_details).Returns(Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle));
    }

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => ObserverService.ResumeReplayForHandlers([_reducerHandler, _otherHandler], _details));

    [Fact] void should_surface_the_missing_attachment() => _error.ShouldBeOfExactType<ReplayInitializationFailed>();
    [Fact] void should_attempt_every_handler() => _otherHandler.Received(1).ResumeReplayFor(_details);
}
