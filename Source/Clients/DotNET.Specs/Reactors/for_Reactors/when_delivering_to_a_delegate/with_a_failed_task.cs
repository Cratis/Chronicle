// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_delivering_to_a_delegate;

public class with_a_failed_task : given.a_registered_delegate
{
    ReactorResult _failure;

    async Task Because()
    {
        var delivery = Task.Run(Deliver);
        await _received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _release.SetException(new Exception("Bridge unavailable"));
        _failure = await _result.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await delivery.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_fail_the_partition() => _failure.State.ShouldEqual(ObservationState.Failed);
    [Fact] void should_not_advance_the_checkpoint() => _failure.LastSuccessfulObservation.ShouldEqual(ulong.MaxValue);
    [Fact] void should_return_the_failure_reason() => _failure.ExceptionMessages.ShouldContain("Bridge unavailable");
}
