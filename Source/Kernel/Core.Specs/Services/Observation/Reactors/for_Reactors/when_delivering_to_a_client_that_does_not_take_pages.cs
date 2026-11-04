// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_delivering_to_a_client_that_does_not_take_pages : given.an_observation_stream
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Because() => _deliver("partition", [], _completion, null, Concepts.Observation.ObservationOrigin.Live);

    [Fact] void should_send_the_partition_on_the_message() => _deliveries[0].Partition.ShouldEqual("partition");
    [Fact] void should_not_send_a_page() => _deliveries[0].Partitions.ShouldBeEmpty();
    [Fact] void should_tell_the_client_the_origin() => _deliveries[0].Origin.ShouldEqual(ObservationOrigin.Live);
}
