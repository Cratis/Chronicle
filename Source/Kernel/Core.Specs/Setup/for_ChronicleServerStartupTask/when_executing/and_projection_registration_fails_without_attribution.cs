// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using NSubstitute.ExceptionExtensions;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_projection_registration_fails_without_attribution : given.a_startup_task
{
    Exception _registrationFailure;
    Exception _exception;

    void Establish()
    {
        var definition = CreateProjectionDefinition("projection", "read-model");
        _registrationFailure = new Exception("Projection service unavailable");
        _projectionsManager.GetProjectionDefinitions().Returns([definition]);
        _projectionsServiceClient
            .Register(_eventStore, Arg.Any<IEnumerable<ProjectionDefinition>>())
            .ThrowsAsync(_registrationFailure);
    }

    async Task Because() => _exception = await Catch.Exception(Execute);

    [Fact] void should_not_fail_startup() => _exception.ShouldBeNull();
    [Fact] void should_report_the_failure_for_the_event_store() => LogEntries.Any(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.DeferredStartupWorkUnitFailed) && _.Exception == _registrationFailure).ShouldBeTrue();
    [Fact] async Task should_retry_once_per_pass() => await _projectionsServiceClient.Received(6).Register(_eventStore, Arg.Any<IEnumerable<ProjectionDefinition>>());
    [Fact] void should_resume_jobs_only_at_startup() => _jobsManager.Received(1).Rehydrate();
}
