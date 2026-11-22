// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ReadinessWait.when_observing;

public class for_a_duration : Cratis.Specifications.Specification
{
    int _lastObserved;

    async Task Because() => _lastObserved = await ReadinessWait.ObserveFor(
        () => Task.FromResult(7), TimeSpan.FromMilliseconds(100));

    [Fact] void should_return_the_last_observation_when_the_deadline_expires() => _lastObserved.ShouldEqual(7);
}
