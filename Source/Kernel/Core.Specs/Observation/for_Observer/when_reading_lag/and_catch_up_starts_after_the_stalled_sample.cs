// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_reading_lag;

public class and_catch_up_starts_after_the_stalled_sample : given.an_observer_publishing_a_stalled_sample
{
    ObserverLagSample _whenCatchUpStarted;
    ObserverLagSample _afterRefreshAndCatchUpEnded;
    ObserverLagSample _afterNewTick;

    async Task Because()
    {
        await _observer.RegisterCatchingUpPartitions(["catching-up"]);
        _whenCatchUpStarted = Published;

        _stateStorage.State.CatchingUpPartitions.Clear();
        await _observer.PartitionFailed("unrelated", 6UL, ["Failure"], "Stack");
        _afterRefreshAndCatchUpEnded = Published;

        await _observer.RunWatchdogAsync();
        _afterNewTick = Published;
    }

    [Fact] void should_withdraw_the_sample_without_a_new_tick() => _whenCatchUpStarted.ShouldBeNull();
    [Fact] void should_not_bring_the_stalled_sample_back_when_the_catch_up_ends() => _afterRefreshAndCatchUpEnded.ShouldBeNull();
    [Fact] void should_publish_again_with_the_next_tick() => _afterNewTick.ShouldNotBeNull();
}
