// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_starting;

public class a_poll_capture : given.a_captures_manager
{
    async Task Because() => await _manager.Start(_poll.Id);

    [Fact] void should_start_the_capturer() => _capturer.Received(1).Start(Arg.Is<Capture>(_ => _.Id == _poll.Id && _.Status == CaptureStatus.Started));
    [Fact] void should_not_subscribe_to_an_inbox() => _subscriptions.DidNotReceive().Subscribe(Arg.Any<EventStoreName>(), Arg.Any<CaptureDefinition>());
}
