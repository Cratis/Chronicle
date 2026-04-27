// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobStatusChangedConverters;

public class when_converting_to_client : Specification
{
    Contracts.Jobs.JobStatusChanged _contract;
    JobStatusChanged _result;
    DateTimeOffset _occurred;

    void Establish()
    {
        _occurred = DateTimeOffset.UtcNow;
        _contract = new Contracts.Jobs.JobStatusChanged
        {
            Status = Contracts.Jobs.JobStatus.Running,
            Occurred = _occurred,
            ExceptionMessages = ["error one", "error two"],
            ExceptionStackTrace = "stack trace here"
        };
    }

    void Because() => _result = _contract.ToClient();

    [Fact] void should_set_status() => _result.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_set_occurred() => _result.Occurred.ShouldEqual(_occurred);
    [Fact] void should_set_exception_messages() => _result.ExceptionMessages.ShouldContainOnly(["error one", "error two"]);
    [Fact] void should_set_exception_stack_trace() => _result.ExceptionStackTrace.ShouldEqual("stack trace here");
}
