// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ReadinessWait.when_observing;

public class for_a_duration_and_a_later_observation_never_completes : Cratis.Specifications.Specification
{
    int _observations;
    int _lastObserved;

    async Task Because() => _lastObserved = await ReadinessWait.ObserveFor(
        () => ++_observations == 1
            ? Task.FromResult(7)
            : new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously).Task,
        TimeSpan.FromMilliseconds(100));

    [Fact] void should_keep_the_last_completed_observation() => _lastObserved.ShouldEqual(7);
}
