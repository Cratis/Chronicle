// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobProgressConverters;

public class when_converting_to_client : Specification
{
    Contracts.Jobs.JobProgress _contract;
    JobProgress _result;

    void Establish() =>
        _contract = new Contracts.Jobs.JobProgress
        {
            TotalSteps = 10,
            SuccessfulSteps = 7,
            FailedSteps = 2,
            StoppedSteps = 1,
            IsCompleted = true,
            IsStopped = false,
            Message = "All done"
        };

    void Because() => _result = _contract.ToClient();

    [Fact] void should_set_total_steps() => _result.TotalSteps.ShouldEqual(10);
    [Fact] void should_set_successful_steps() => _result.SuccessfulSteps.ShouldEqual(7);
    [Fact] void should_set_failed_steps() => _result.FailedSteps.ShouldEqual(2);
    [Fact] void should_set_stopped_steps() => _result.StoppedSteps.ShouldEqual(1);
    [Fact] void should_set_is_completed() => _result.IsCompleted.ShouldBeTrue();
    [Fact] void should_set_is_stopped() => _result.IsStopped.ShouldBeFalse();
    [Fact] void should_set_message() => _result.Message.ShouldEqual(new JobProgressMessage("All done"));
}
