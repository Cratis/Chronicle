// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_recovery_finishes_after_only_partially_recovering : given.a_webhook_observer_with_an_overdue_deferral
{
    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, SubscriberTimeout = 0 };

    async Task Because()
    {
        await Remind();
        await _observer.FailedPartitionPartiallyRecovered("partition", 42UL, _failure.Id, _deferral.Id);
        await Remind();
        await Remind();
    }

    [Fact] void should_start_a_new_recovery_once() => _startCalls.ShouldEqual(2);
}
