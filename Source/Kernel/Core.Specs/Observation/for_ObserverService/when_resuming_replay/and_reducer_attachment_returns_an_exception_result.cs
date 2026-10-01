// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ObserverService.when_resuming_replay;

public class and_reducer_attachment_returns_an_exception_result : Specification
{
    ICanHandleReplayForObserver _handler;
    ObserverDetails _details;
    Exception _error;

    void Establish()
    {
        _details = new(new("reducer", "store", "namespace", "event-log"), ObserverType.Reducer);
        _handler = Substitute.For<ICanHandleReplayForObserver>();
        _handler.ResumeReplayFor(_details).Returns(Result.Failed(ICanHandleReplayForObserver.Error.Unknown));
    }

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => ObserverService.ResumeReplayForHandlers([_handler], _details));

    [Fact] void should_surface_the_failed_attachment() => _error.ShouldBeOfExactType<ReplayInitializationFailed>();
}
