// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_delivering_to_a_client_that_takes_pages : given.a_page_observation_stream
{
    readonly TaskCompletionSource<ReducerSubscriberResult> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Because() => _deliver(new("partition", [], null) { Origin = Concepts.Observation.ObservationOrigin.CatchUp }, _completion);

    [Fact] void should_send_the_events_as_a_page_with_one_partition() => _deliveries[0].Partitions.Single().Partition.ShouldEqual("partition");
    [Fact] void should_send_no_initial_state_when_there_is_none() => _deliveries[0].Partitions.Single().InitialState.ShouldBeNull();
    [Fact] void should_not_send_the_single_partition_fields() => _deliveries[0].Partition.ShouldBeNull();
    [Fact] void should_tell_the_client_the_origin() => _deliveries[0].Origin.ShouldEqual(ObservationOrigin.CatchingUp);
}
