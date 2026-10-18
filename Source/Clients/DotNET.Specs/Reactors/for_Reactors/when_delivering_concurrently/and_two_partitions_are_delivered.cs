// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_delivering_concurrently;

public class and_two_partitions_are_delivered : given.a_delegate_with_gated_batches
{
    bool _firstAnsweredWhenSecondWas;
    ReactorResult _first;
    ReactorResult _second;

    async Task Because()
    {
        Deliver("order-1", 1, 101);
        Deliver("order-2", 2, 102);
        Release("order-2", 2);
        await Started("order-1", 1);
        _second = await Answered(102);
        _firstAnsweredWhenSecondWas = IsAnswered(101);
        Release("order-1", 1);
        _first = await Answered(101);
    }

    [Fact] void should_answer_the_second_partition_while_the_first_is_still_running() => _firstAnsweredWhenSecondWas.ShouldBeFalse();
    [Fact] void should_answer_the_first_delivery_for_the_first_partition() => _first.Partition.ShouldEqual("order-1");
    [Fact] void should_answer_the_second_delivery_for_the_second_partition() => _second.Partition.ShouldEqual("order-2");
    [Fact] void should_report_the_first_partitions_position() => _first.LastSuccessfulObservation.ShouldEqual(1UL);
    [Fact] void should_report_the_second_partitions_position() => _second.LastSuccessfulObservation.ShouldEqual(2UL);
}
