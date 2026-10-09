// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring;

public class an_events_capture_that_cannot_subscribe : given.a_captures_manager
{
    void Establish()
    {
        StartedState(_events);
        StartedState(_poll);
        _subscriptions.Subscribe(_eventStore, Arg.Any<CaptureDefinition>()).Returns<Task>(_ => throw new TimeoutException());
    }

    async Task Because() => await _manager.Ensure();

    [Fact] void should_still_resume_the_other_captures() => _capturer.Received(1).Start(Arg.Is<Capture>(_ => _.Id == _poll.Id));
    [Fact] void should_leave_the_failed_capture_started_so_reconciliation_retries_it() => _captures.DidNotReceive().Save(Arg.Any<Capture>());
}
