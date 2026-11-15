// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverInformation.when_observing_observers;

public class and_a_definition_is_added : given.observed_streams
{
    void Because()
    {
        _states.OnNext([]);
        _definitions.OnNext([Definition("first"), Definition("second")]);
    }

    [Fact] void should_emit_the_definition_change() => _emissions.Count.ShouldEqual(2);
    [Fact] void should_include_the_new_definition() => _emissions[^1].Single(_ => _.Id == "second").RunningState.ShouldEqual(Contracts.Observation.ObserverRunningState.Unknown);
}
