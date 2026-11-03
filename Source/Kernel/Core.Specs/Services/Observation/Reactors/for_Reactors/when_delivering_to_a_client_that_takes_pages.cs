// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_delivering_to_a_client_that_takes_pages : given.a_page_observation_stream
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Because() => _deliver("partition", [], _completion, null, Concepts.Observation.ObservationOrigin.Replay);

    [Fact] void should_send_the_events_as_a_page_with_one_partition() => _deliveries[0].Partitions.Single().Partition.ShouldEqual("partition");
    [Fact] void should_not_send_the_single_partition_fields() => _deliveries[0].Partition.ShouldBeNull();
    [Fact] void should_tell_the_client_the_origin() => _deliveries[0].Origin.ShouldEqual(ObservationOrigin.Replaying);
}
