// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_retry_reminder_arrives_after_the_job_failed_to_start : given.a_webhook_observer_with_an_overdue_deferral
{
    void Establish() => _startResult = () => _startCalls == 1 ? null : Result<JobId, StartJobError>.Success(JobId.New());

    async Task Because()
    {
        await Remind();
        await Remind();
    }

    [Fact] void should_try_to_start_again() => _startCalls.ShouldEqual(2);
}
