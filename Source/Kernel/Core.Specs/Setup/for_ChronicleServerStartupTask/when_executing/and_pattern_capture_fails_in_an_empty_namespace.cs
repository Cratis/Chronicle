// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_pattern_capture_fails_in_an_empty_namespace : given.a_startup_task_with_an_empty_namespace
{
    Exception _error;

    void Establish() => _patternCapture.Subscribe(_eventStore, _emptyNamespace)
        .Returns(_ => Task.FromException(new InvalidOperationException("Capture is unavailable")));

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_complete_startup() => _error.ShouldBeNull();
    [Fact] async Task should_not_retry_a_non_transient_failure() => await _patternCapture.Received(1).Subscribe(_eventStore, _emptyNamespace);
    [Fact] async Task should_subscribe_the_other_namespace() => await _patternCapture.Received(1).Subscribe(_eventStore, _namespace);
    [Fact] async Task should_rehydrate_jobs_in_the_other_namespace() => await _jobsManager.Received(1).Rehydrate();
    [Fact] async Task should_rehydrate_event_sequences_in_the_other_namespace() => await _eventSequences.Received(1).Rehydrate();
    [Fact] void should_run_the_final_authentication_step() => _bootstrapClientsEnsured.ShouldBeTrue();
}
