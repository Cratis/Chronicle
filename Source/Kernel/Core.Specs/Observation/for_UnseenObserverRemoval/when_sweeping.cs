// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_UnseenObserverRemoval;

public class when_sweeping : given.an_unseen_observer_removal
{
    int _result;

    async Task Because() => _result = await _removal.Sweep();

    [Fact] void should_report_the_removed_observers() => _result.ShouldEqual(1);
    [Fact] void should_remove_an_observer_no_client_has_used_for_the_period() => _removed.ShouldContain("stale");
    [Fact] void should_remove_nothing_else() => _removed.Count.ShouldEqual(1);
    [Fact] void should_keep_an_observer_seen_within_the_period() => _removed.ShouldNotContain("recent");
    [Fact] void should_keep_an_observer_seen_recently_in_any_namespace() => _removed.ShouldNotContain("seen-in-one-namespace");
    [Fact] void should_keep_an_observer_that_has_no_recorded_time() => _removed.ShouldNotContain("never-seen");
    [Fact] async Task should_start_the_clock_of_an_observer_that_has_no_recorded_time() =>
        (await _states[First].GetLastSeenAt(["never-seen"], default))["never-seen"].ShouldEqual(_time.Now);
    [Fact] void should_keep_a_subscribed_observer() => _removed.ShouldNotContain("subscribed");
    [Fact] async Task should_record_that_a_subscribed_observer_was_just_seen() =>
        (await _states[First].GetLastSeenAt(["subscribed"], default))["subscribed"].ShouldEqual(_time.Now);
    [Fact] void should_keep_a_replaying_observer() => _removed.ShouldNotContain("replaying");
    [Fact] void should_keep_a_quarantined_observer() => _removed.ShouldNotContain("quarantined");
    [Fact] void should_keep_an_observer_with_failed_partitions() => _removed.ShouldNotContain("with-failed-partition");
    [Fact] void should_keep_an_observer_with_an_unfinished_job() => _removed.ShouldNotContain("with-job");
    [Fact] void should_keep_a_kernel_owned_observer() => _removed.ShouldNotContain("kernel-owned");
    [Fact] void should_keep_an_observer_that_is_not_a_reactor_or_reducer() => _removed.ShouldNotContain("webhook");
    [Fact] void should_not_activate_observers_it_did_not_consider_for_removal() =>
        _grains
            .Where(grain => grain.Key.StartsWith("recent#", StringComparison.Ordinal) || grain.Key.StartsWith("never-seen#", StringComparison.Ordinal))
            .All(grain => !grain.Value.ReceivedCalls().Any())
            .ShouldBeTrue();

    protected override async Task Seed()
    {
        await Observer("stale", Old, Old);
        await Observer("recent", Recent, Recent);
        await Observer("seen-in-one-namespace", Old, Recent);
        await Observer("never-seen", null, null);
        await Observer("subscribed", Old, Old, isSubscribed: true);
        await Observer("replaying", Old, Old, runningState: ObserverRunningState.Replaying);
        await Observer("quarantined", Old, Old, runningState: ObserverRunningState.Quarantined);
        await Observer("with-failed-partition", Old, Old, hasFailedPartition: true);
        await Observer("with-job", Old, Old, hasJob: true);
        await Observer("kernel-owned", Old, Old, owner: ObserverOwner.Kernel, type: ObserverType.Projection);
        await Observer("webhook", Old, Old, owner: ObserverOwner.Kernel, type: ObserverType.External);
    }
}
