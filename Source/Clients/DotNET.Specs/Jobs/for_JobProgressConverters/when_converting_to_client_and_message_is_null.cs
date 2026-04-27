// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobProgressConverters;

public class when_converting_to_client_and_message_is_null : Specification
{
    Contracts.Jobs.JobProgress _contract;
    JobProgress _result;

    void Establish() =>
        _contract = new Contracts.Jobs.JobProgress
        {
            TotalSteps = 3,
            Message = null!
        };

    void Because() => _result = _contract.ToClient();

    [Fact] void should_not_throw() => _result.ShouldNotBeNull();
    [Fact] void should_use_empty_message() => _result.Message.ShouldEqual(JobProgressMessage.None);
}
