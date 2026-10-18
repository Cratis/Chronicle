// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_receiving_a_duplicate_result : given.an_observation_stream
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _original = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ObserverSubscriberResult> _next = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception _exception;

    void Establish()
    {
        _deliver("partition", [], _original);
        Reply(_deliveries[0].DeliveryId);
        _deliver("partition", [], _next);
    }

    void Because() => _exception = Catch.Exception(() => Reply(_deliveries[0].DeliveryId));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_not_complete_the_next_delivery() => _next.Task.IsCompleted.ShouldBeFalse();
}
