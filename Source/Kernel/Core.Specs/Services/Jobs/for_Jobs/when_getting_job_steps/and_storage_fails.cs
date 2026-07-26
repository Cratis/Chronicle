// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Jobs.for_Jobs.when_getting_job_steps;

public class and_storage_fails : given.all_dependencies
{
    IEnumerable<JobStep> _result;

    async Task Because() => _result = await _service.GetJobSteps(new GetJobStepsRequest
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        JobId = Guid.NewGuid(),
        Statuses = []
    });

    [Fact] void should_return_empty_steps() => _result.ShouldBeEmpty();
    [Fact] void should_log_the_failure_as_error() => _logger.Entries.ShouldContain(LogLevel.Error);
}
