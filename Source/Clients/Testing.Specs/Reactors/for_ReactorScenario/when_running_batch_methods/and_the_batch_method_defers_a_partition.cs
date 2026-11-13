// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_running_batch_methods;

public class and_the_batch_method_defers_a_partition : given.a_seat_index_scenario
{
    EventSourceId _id;

    void Establish() => _id = EventSourceId.New();

    async Task Because() => await _scenario.Given.ForEventSource(_id).Events(new SeatBooked("later"));

    [Fact] void should_report_the_partition_as_deferred() => _scenario.ShouldHaveDeferred(_id);
    [Fact] void should_carry_the_delay() => _scenario.PartitionOutcomes[0].RetryAfter.ShouldEqual(TimeSpan.FromSeconds(5));
    [Fact] void should_not_report_the_partition_as_failed() => Catch.Exception(() => _scenario.ShouldHaveFailed(_id)).ShouldBeOfExactType<BatchPartitionOutcomeAssertionException>();
}
