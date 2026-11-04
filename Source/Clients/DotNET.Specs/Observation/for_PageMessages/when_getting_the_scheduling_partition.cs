// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Observation.for_PageMessages;

public class when_getting_the_scheduling_partition : Specification
{
    [Fact] void should_use_the_partition_of_a_single_partition_message() => new EventsToObserve { Partition = "a" }.SchedulingPartition().ShouldEqual("a");
    [Fact] void should_use_the_partition_of_a_page_with_one_partition() => new EventsToObserve { Partitions = [new() { Partition = "b" }] }.SchedulingPartition().ShouldEqual("b");
    [Fact] void should_have_none_for_a_page_with_several_partitions() => new EventsToObserve { Partitions = [new() { Partition = "a" }, new() { Partition = "b" }] }.SchedulingPartition().ShouldBeNull();
}
