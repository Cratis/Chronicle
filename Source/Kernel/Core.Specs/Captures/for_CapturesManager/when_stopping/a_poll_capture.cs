// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_stopping;

public class a_poll_capture : given.a_captures_manager
{
    void Establish() => StartedState(_poll);

    async Task Because() => await _manager.Stop(_poll.Id);

    [Fact] void should_stop_the_capturer() => _capturer.Received(1).Stop();
    [Fact] void should_not_unsubscribe_from_an_inbox() => _subscriptions.DidNotReceive().Unsubscribe(Arg.Any<EventStoreName>(), Arg.Any<CaptureDefinition>());
}
