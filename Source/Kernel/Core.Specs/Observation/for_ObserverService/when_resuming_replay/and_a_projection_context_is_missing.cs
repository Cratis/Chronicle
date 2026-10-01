// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_ObserverService.when_resuming_replay;

public class and_a_projection_context_is_missing : Specification
{
    ICanHandleReplayForObserver _handler;
    ObserverDetails _details;
    Exception? _error;
    void Establish()
    {
        _details = new(new("projection", "store", "namespace", "event-log"), ObserverType.Projection);
        _handler = Substitute.For<ICanHandleReplayForObserver>();
        _handler.ResumeReplayFor(_details).Returns(Result.Failed(ICanHandleReplayForObserver.Error.CouldNotGetReplayContext));
    }
    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => ObserverService.ResumeReplayForHandlers([_handler], _details));
    [Fact] void should_preserve_the_existing_projection_resume_behavior() => _error.ShouldBeNull();
}
