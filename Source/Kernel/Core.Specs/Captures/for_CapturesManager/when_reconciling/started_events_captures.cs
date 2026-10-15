// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_reconciling;

public class started_events_captures : given.a_captures_manager
{
    void Establish()
    {
        StartedState(_events);
        StartedState(_poll);
    }

    async Task Because() => await _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_recover_the_events_capture() => _subscriptions.Received(1).Recover(_eventStore, Arg.Is<CaptureDefinition>(_ => _.Id == _events.Id));
    [Fact] void should_only_recover_events_captures() => _subscriptions.Received(1).Recover(Arg.Any<EventStoreName>(), Arg.Any<CaptureDefinition>());
}
