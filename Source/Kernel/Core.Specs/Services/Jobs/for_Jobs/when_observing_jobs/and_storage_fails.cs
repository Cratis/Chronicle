// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Queries;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Jobs.for_Jobs.when_observing_jobs;

public class and_storage_fails : given.all_dependencies
{
    QueryResult<IEnumerable<JobSummaryResponse>> _observed;

    void Because() => _service.AllJobs(new AllJobsRequest
    {
        EventStore = "test-store",
        Namespace = "test-namespace"
    }).Subscribe(result => _observed = result);

    [Fact] void should_emit_a_result_instead_of_completing_empty() => _observed.ShouldNotBeNull();
    [Fact] void should_not_report_success() => _observed.IsSuccess.ShouldBeFalse();
    [Fact] void should_surface_the_storage_exception_message() => _observed.ExceptionMessages.ShouldContain(_exception.Message);
    [Fact] void should_log_the_failure_as_error() => _logger.Entries.ShouldContain(LogLevel.Error);
}
