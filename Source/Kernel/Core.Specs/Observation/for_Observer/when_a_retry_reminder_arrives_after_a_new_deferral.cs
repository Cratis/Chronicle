// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_retry_reminder_arrives_after_a_new_deferral : given.a_webhook_observer_with_an_overdue_deferral
{
    async Task Because()
    {
        await Remind();
        await _observer.PartitionFailed("partition", 42UL, ["timed out"], string.Empty, FailureKind.Timeout, _failure.Id, _deferral.Id);
        _failure.Deferral.ShouldNotBeNull();
        _failure.Deferral!.Id.ShouldNotEqual(_deferral.Id);
        _failure.Deferral = _failure.Deferral with { Until = DateTimeOffset.UtcNow.AddMinutes(-1) };
        await Remind();
    }

    [Fact] void should_start_a_recovery_for_the_new_deferral() => _startCalls.ShouldEqual(2);
}
