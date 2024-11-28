// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Observation.States.for_Observing;

public class when_leaving : given.an_observing_state
{
    async Task Establish() => await _state.OnEnter(_storedState);

    async Task Because() => await _state.OnLeave(_storedState);

    [Fact] void should_unsubscribe_from_stream() => _appendedEventsQueues.Received(1).Unsubscribe(_queueSubscription);
}
