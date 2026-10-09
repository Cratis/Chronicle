// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_deleting;

public class a_poll_capture : given.a_captures_manager
{
    async Task Because() => await _manager.Delete(_poll.Id);

    [Fact] void should_stop_the_capturer() => _capturer.Received(1).Stop();
    [Fact] void should_delete_the_capture() => _captures.Received(1).Delete(_poll.Id);
    [Fact] void should_not_remove_inbox_state() => _subscriptions.DidNotReceive().Remove(Arg.Any<EventStoreName>(), Arg.Any<CaptureDefinition>());
}
