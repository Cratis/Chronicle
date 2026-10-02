// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_pattern_capture_exhausts_retries_in_an_empty_namespace : given.a_startup_task_with_an_empty_namespace
{
    Exception _error;

    void Establish() => _patternCapture.Subscribe(_eventStore, _emptyNamespace)
        .Returns(_ => Task.FromException(new TimeoutException("Sibling silo is unavailable")));

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_complete_startup() => _error.ShouldBeNull();
    [Fact] async Task should_exhaust_the_transient_retry_budget() => await _patternCapture.Received(5).Subscribe(_eventStore, _emptyNamespace);
    [Fact] async Task should_subscribe_the_other_namespace() => await _patternCapture.Received(1).Subscribe(_eventStore, _namespace);
    [Fact] async Task should_rehydrate_jobs_in_the_other_namespace() => await _jobsManager.Received(1).Rehydrate();
    [Fact] void should_run_the_final_authentication_step() => _bootstrapClientsEnsured.ShouldBeTrue();
}
