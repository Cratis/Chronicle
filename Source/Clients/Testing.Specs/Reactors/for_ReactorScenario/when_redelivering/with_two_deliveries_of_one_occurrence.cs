// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_redelivering;

public class with_two_deliveries_of_one_occurrence : a_recorded_occurrence
{
    readonly List<ReactorDelivery> _deliveries = [];

    void Establish() => _occurrenceRecorder.WhenForAnyArgs(recorder => recorder.Record(default!, default!, default!))
        .Do(call => _deliveries.Add(call.Arg<ReactorDelivery>()));

    async Task Because()
    {
        await _scenario.Redeliver(_occurrence);
        await _scenario.Redeliver(_occurrence);
    }

    [Fact] void should_invoke_the_handler_for_each_delivery() => _deliveries.Count.ShouldEqual(2);
    [Fact] void should_receive_equal_deliveries() => _deliveries[1].ShouldEqual(_deliveries[0]);
    [Fact] void should_receive_the_same_delivery_id() => _deliveries[1].Id.ShouldEqual(_deliveries[0].Id);
}
