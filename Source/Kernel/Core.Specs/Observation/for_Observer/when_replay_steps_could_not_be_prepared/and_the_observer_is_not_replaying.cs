// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_steps_could_not_be_prepared;

public class and_the_observer_is_not_replaying : for_Observer.given.an_observer_with_subscription
{
    void Establish() => _storageStats.ResetCounts();

    async Task Because() => await _observer.ReplayStepsCouldNotBePrepared();

    [Fact] async Task should_not_be_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
}
