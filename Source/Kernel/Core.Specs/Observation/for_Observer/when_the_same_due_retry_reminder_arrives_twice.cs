// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_the_same_due_retry_reminder_arrives_twice : given.a_webhook_observer_with_an_overdue_deferral
{
    async Task Because()
    {
        await Remind();
        await Remind();
    }

    [Fact] void should_start_the_recovery_once() => _startCalls.ShouldEqual(1);
}
