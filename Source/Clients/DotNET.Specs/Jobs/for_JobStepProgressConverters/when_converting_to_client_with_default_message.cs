// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobStepProgressConverters;

public class when_converting_to_client_with_default_message : Specification
{
    JobStepProgress _result;

    void Because() => _result = new Contracts.Jobs.JobStepProgress().ToClient();

    [Fact] void should_use_empty_message() => _result.Message.ShouldEqual(JobStepProgressMessage.None);
}
