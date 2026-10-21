// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_receiving_a_late_result : given.an_observation_stream
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _original = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ObserverSubscriberResult> _retry = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _deliver("partition", [], _original);

        // The caller has abandoned its wait, but the original delivery still needs its own answer.
        _deliver("partition", [], _retry);
    }

    void Because() => Reply();

    [Fact] void should_complete_the_original_delivery() => _original.Task.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_not_complete_the_retry() => _retry.Task.IsCompleted.ShouldBeFalse();
}
