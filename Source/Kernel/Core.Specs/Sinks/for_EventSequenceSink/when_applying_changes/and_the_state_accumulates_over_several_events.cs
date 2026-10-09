// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

public class and_the_state_accumulates_over_several_events : given.a_sink
{
    System.Dynamic.ExpandoObject _state;

    async Task Because()
    {
        await _sink.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5);
        var initial = await _sink.FindOrDefault("customer-1");
        await _sink.ApplyChanges("customer-1", Fold(initial!, 25, 2, 9), 9);
        _state = (await _sink.FindOrDefault("customer-1"))!;
    }

    [Fact] void should_publish_each_new_state_as_a_new_instance() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).Count.ShouldEqual(2);
    [Fact] void should_read_back_the_latest_state() => TotalOf(_state).ShouldEqual("25");
}
