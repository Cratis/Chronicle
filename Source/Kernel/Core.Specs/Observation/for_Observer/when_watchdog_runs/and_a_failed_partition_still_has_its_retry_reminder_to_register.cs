// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// With coalesced writes, the retry reminder of a failed partition is registered by the next flush, which has not run.
/// </summary>
public class and_a_failed_partition_still_has_its_retry_reminder_to_register : given.an_idle_observer
{
    protected override int WriteCoalescingInterval => 3600;

    async Task Establish() => await _observer.PartitionFailed("partition", 42UL, ["Failed"], "Stack");

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_idlePeriod * 2);
    }

    [Fact] void should_keep_the_activation_until_the_reminder_is_registered() => ReleaseRequests.ShouldEqual(0);
}
