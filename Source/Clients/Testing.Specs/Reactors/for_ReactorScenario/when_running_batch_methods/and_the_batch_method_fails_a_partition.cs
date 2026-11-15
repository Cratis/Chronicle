// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_running_batch_methods;

public class and_the_batch_method_fails_a_partition : given.a_seat_index_scenario
{
    EventSourceId _failing;
    EventSourceId _other;

    void Establish()
    {
        _failing = EventSourceId.New();
        _other = EventSourceId.New();
    }

    async Task Because() => await _scenario.Given.Page(
        new(_other, new SeatBooked("3C")),
        new(_failing, new SeatBooked("unavailable")));

    [Fact] void should_report_the_partition_as_failed() => _scenario.ShouldHaveFailed(_failing);
    [Fact] void should_report_only_that_partition() => _scenario.PartitionOutcomes.Select(_ => _.Partition).ShouldContainOnly(_failing);
    [Fact] void should_carry_the_exception() => _scenario.PartitionOutcomes[0].Exception.ShouldBeOfExactType<SeatUnavailable>();
    [Fact] void should_fail_at_the_failing_event() => _scenario.PartitionOutcomes[0].At.ShouldEqual((EventSequenceNumber)1UL);
    [Fact] void should_not_report_the_partition_as_deferred() => Catch.Exception(() => _scenario.ShouldHaveDeferred(_failing)).ShouldBeOfExactType<BatchPartitionOutcomeAssertionException>();
}
