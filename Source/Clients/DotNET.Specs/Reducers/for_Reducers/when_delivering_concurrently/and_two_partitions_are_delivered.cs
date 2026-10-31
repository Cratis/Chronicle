// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_delivering_concurrently;

public class and_two_partitions_are_delivered : given.a_reducer_with_gated_partitions
{
    bool _firstAnsweredWhenSecondWas;
    ReducerResult _first;
    ReducerResult _second;

    async Task Because()
    {
        Deliver("order-1", 1, 101);
        Deliver("order-2", 2, 102);
        Release("order-2");
        await Started("order-1");
        _second = await Answered(102);
        _firstAnsweredWhenSecondWas = IsAnswered(101);
        Release("order-1");
        _first = await Answered(101);
    }

    [Fact] void should_answer_the_second_partition_while_the_first_is_still_running() => _firstAnsweredWhenSecondWas.ShouldBeFalse();
    [Fact] void should_answer_the_first_delivery_for_the_first_partition() => _first.Partition.ShouldEqual("order-1");
    [Fact] void should_answer_the_second_delivery_for_the_second_partition() => _second.Partition.ShouldEqual("order-2");
}
