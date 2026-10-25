// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_manually_retrying_a_partition_whose_recovery_started : given.a_webhook_observer_with_an_overdue_deferral
{
    async Task Because()
    {
        await Remind();
        await _observer.TryStartRecoverJobForFailedPartition("partition");
    }

    [Fact] void should_start_the_manual_recovery() => _startCalls.ShouldEqual(2);
}
