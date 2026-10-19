// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_receiving_an_identified_result : given.an_observation_stream
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _original = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ObserverSubscriberResult> _retry = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _deliver("partition", [], _original);
        _deliver("partition", [], _retry);
    }

    void Because() => Reply(_deliveries[1].DeliveryId);

    [Fact] void should_assign_nonzero_ids_starting_at_one() => _deliveries[0].DeliveryId.ShouldEqual(1UL);
    [Fact] void should_assign_distinct_ids_to_retries() => _deliveries[1].DeliveryId.ShouldEqual(2UL);
    [Fact] void should_complete_only_the_answered_delivery() => _retry.Task.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_leave_the_original_pending() => _original.Task.IsCompleted.ShouldBeFalse();
}
