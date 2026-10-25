// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_stale_recovery_reports_it_did_not_recover : given.a_webhook_observer_with_an_overdue_deferral
{
    async Task Because()
    {
        await Remind();
        await _observer.FailedPartitionNotRecovered("partition", FailedPartitionId.New(), _deferral.Id);
        await _observer.FailedPartitionNotRecovered("partition", _failure.Id, Guid.NewGuid());
        await _observer.FailedPartitionPartiallyRecovered("partition", 42UL, FailedPartitionId.New(), _deferral.Id);
        await _observer.FailedPartitionPartiallyRecovered("partition", 42UL, _failure.Id, Guid.NewGuid());
        await Remind();
    }

    [Fact] void should_keep_the_current_recovery_in_flight() => _startCalls.ShouldEqual(1);
}
