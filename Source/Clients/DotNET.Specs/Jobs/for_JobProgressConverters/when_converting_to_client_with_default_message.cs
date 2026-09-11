// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobProgressConverters;

public class when_converting_to_client_with_default_message : Specification
{
    JobProgress _result;

    void Because() => _result = new Contracts.Jobs.JobProgress().ToClient();

    [Fact] void should_use_empty_message() => _result.Message.ShouldEqual(JobProgressMessage.None);
}
