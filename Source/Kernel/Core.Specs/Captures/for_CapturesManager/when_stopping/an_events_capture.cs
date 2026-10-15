// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_stopping;

public class an_events_capture : given.a_captures_manager
{
    void Establish() => StartedState(_events);

    async Task Because() => await _manager.Stop(_events.Id);

    [Fact] void should_unsubscribe_from_the_inbox() => _subscriptions.Received(1).Unsubscribe(_eventStore, Arg.Is<CaptureDefinition>(_ => _.Id == _events.Id));
    [Fact] void should_record_it_as_stopped() => _captures.Received(1).Save(Arg.Is<Capture>(_ => _.Status == CaptureStatus.Stopped));
    [Fact] void should_not_stop_the_poll_capturer() => _capturer.DidNotReceive().Stop();
}
