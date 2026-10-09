// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring;

public class started_captures : given.a_captures_manager
{
    void Establish()
    {
        StartedState(_events);
        StartedState(_poll);
    }

    async Task Because() => await _manager.Ensure();

    [Fact] void should_subscribe_the_events_capture() => _subscriptions.Received(1).Subscribe(_eventStore, Arg.Is<CaptureDefinition>(_ => _.Id == _events.Id));
    [Fact] void should_start_the_poll_capturer() => _capturer.Received(1).Start(Arg.Is<Capture>(_ => _.Id == _poll.Id));
}
