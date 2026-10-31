// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_receiving_a_late_result : given.an_observation_stream
{
    readonly TaskCompletionSource<ReducerSubscriberResult> _original = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ReducerSubscriberResult> _retry = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _deliver(new("partition", [], null), _original);
        _deliver(new("partition", [], null), _retry);
    }

    void Because() => Reply();

    [Fact] void should_complete_the_original_delivery() => _original.Task.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_not_complete_the_retry() => _retry.Task.IsCompleted.ShouldBeFalse();
}
