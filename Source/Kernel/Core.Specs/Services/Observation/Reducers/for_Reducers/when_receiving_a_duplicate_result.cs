// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_receiving_a_duplicate_result : given.an_observation_stream
{
    readonly TaskCompletionSource<ReducerSubscriberResult> _original = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ReducerSubscriberResult> _next = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception _exception;

    void Establish()
    {
        _deliver(new("partition", [], null), _original);
        Reply(_deliveries[0].DeliveryId);
        _deliver(new("partition", [], null), _next);
    }

    void Because() => _exception = Catch.Exception(() => Reply(_deliveries[0].DeliveryId));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_not_complete_the_next_delivery() => _next.Task.IsCompleted.ShouldBeFalse();
}
