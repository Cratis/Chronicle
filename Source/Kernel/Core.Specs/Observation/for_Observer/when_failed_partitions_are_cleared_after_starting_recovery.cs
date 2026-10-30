// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_failed_partitions_are_cleared_after_starting_recovery : given.a_webhook_observer_with_an_overdue_deferral
{
    async Task Because()
    {
        await Remind();
        await _observer.ClearFailedPartitions();
    }

    [Fact] void should_have_started_recovery() => _startCalls.ShouldEqual(1);
    [Fact] void should_forget_the_recovery_start() => ((IDictionary)typeof(Observer).GetField("_recoveryStarts", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_observer)!).Count.ShouldEqual(0);
}
