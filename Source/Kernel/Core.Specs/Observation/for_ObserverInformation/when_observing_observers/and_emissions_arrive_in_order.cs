// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;

namespace Cratis.Chronicle.Observation.for_ObserverInformation.when_observing_observers;

public class and_emissions_arrive_in_order : given.observed_streams
{
    ObserverInformation[] _latest;

    void Because()
    {
        _states.OnNext([]);
        _definitions.OnNext([Definition("second")]);
        _states.OnNext([new() { Identifier = "second", LastHandledEventSequenceNumber = 42 }]);
        using var subscription = _result.Subscribe(observers => _latest = observers.ToArray());
    }

    [Fact] void should_replay_the_latest_definition() => _latest.Single().Id.ShouldEqual("second");
    [Fact] void should_replay_the_latest_state() => _latest.Single().LastHandledEventSequenceNumber.ShouldEqual(42UL);
}
