// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ReplayObserver.when_handling;

public class and_no_event_sequence_is_given_for_an_inbox_observer : given.a_replay_command
{
    async Task Because() => _result = await _scenario.Execute(new ReplayObserver(
        "some-event-store", "some-namespace", "some-observer", string.Empty));

    [Fact] void should_address_the_observers_inbox() =>
        _grainFactory.Received(1).GetGrain<IObserver>(_key);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
}
