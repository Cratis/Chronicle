// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page;

public class and_one_partition_is_deferred_by_the_client : given.a_page_pipeline
{
    void Establish() => _clientResults["first"] = new(ObserverSubscriberState.Deferred, EventSequenceNumber.Unavailable, [], string.Empty);

    Task Because() => ReduceThePage();

    [Fact] void should_write_no_state_for_the_deferred_partition() => _sink.Writes.ShouldNotContain("first");
    [Fact] void should_write_the_other_partitions() => _sink.Writes.ShouldContainOnly("second", "third");
}
