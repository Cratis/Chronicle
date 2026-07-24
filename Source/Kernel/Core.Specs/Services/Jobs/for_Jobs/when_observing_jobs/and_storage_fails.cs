// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Jobs.for_Jobs.when_observing_jobs;

public class and_storage_fails : given.all_dependencies
{
    IObservable<IEnumerable<Job>> _observable;
    Exception _observedError;

    void Because()
    {
        _observable = _service.ObserveJobs(new GetJobsRequest
        {
            EventStore = "test-store",
            Namespace = "test-namespace"
        });

        _observable.Subscribe(
            _ => { },
            ex => _observedError = ex,
            () => { });
    }

    [Fact] void should_error_the_observable_instead_of_completing_empty() => _observedError.ShouldNotBeNull();
    [Fact] void should_surface_the_storage_exception() => _observedError.ShouldEqual(_exception);
    [Fact] void should_log_the_failure_as_error() => _logger.Entries.ShouldContain(LogLevel.Error);
}
