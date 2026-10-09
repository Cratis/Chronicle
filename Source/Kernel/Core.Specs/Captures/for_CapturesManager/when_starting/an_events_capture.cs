// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_starting;

public class an_events_capture : given.a_captures_manager
{
    IEnumerable<CaptureValidationMessage> _result;

    async Task Because() => _result = await _manager.Start(_events.Id);

    [Fact] void should_be_started() => _result.ShouldBeEmpty();
    [Fact] void should_subscribe_to_the_inbox() => _subscriptions.Received(1).Subscribe(_eventStore, Arg.Is<CaptureDefinition>(_ => _.Id == _events.Id));
    [Fact] void should_subscribe_before_recording_it_as_started() => _calls.ShouldEqual(["subscribe", "save:Started"]);
    [Fact] void should_not_start_the_poll_capturer() => _capturer.DidNotReceive().Start(Arg.Any<Capture>());
}
