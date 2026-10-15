// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_deleting;

public class an_events_capture : given.a_captures_manager
{
    void Establish() => StartedState(_events);

    async Task Because() => await _manager.Delete(_events.Id);

    [Fact] void should_remove_its_observers_and_state() => _subscriptions.Received(1).Remove(_eventStore, Arg.Is<CaptureDefinition>(_ => _.Id == _events.Id));
    [Fact] void should_delete_the_capture() => _captures.Received(1).Delete(_events.Id);
    [Fact] void should_not_stop_the_poll_capturer() => _capturer.DidNotReceive().Stop();
}
