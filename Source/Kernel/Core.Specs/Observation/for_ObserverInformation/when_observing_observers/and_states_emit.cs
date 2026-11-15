// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverInformation.when_observing_observers;

public class and_states_emit : given.observed_streams
{
    void Because()
    {
        for (var i = 0; i < 5; i++) _states.OnNext([]);
    }

    [Fact] void should_not_read_definitions() => _definitionsStorage.DidNotReceive().GetAll();
    [Fact] void should_emit_for_every_state() => _emissions.Count.ShouldEqual(5);
}
