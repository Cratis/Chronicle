// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Backlog work never stops the host, whatever the failure. A non-transient failure of a background step is retried
/// in the background rather than failing startup, because a restart would meet the same state and fail the same way.
/// </summary>
public class and_a_background_step_fails_for_a_reason_other_than_the_cluster : given.a_startup_task
{
    int _calls;
    Exception _error;

    void Establish() =>
        _jobsManager.Rehydrate()
            .Returns(_ => ++_calls <= 2 ? Task.FromException(new InvalidOperationException("something is wrong")) : Task.CompletedTask);

    async Task Because() => _error = await Catch.Exception(ExecuteAndSettle);

    [Fact] void should_not_fail_startup() => _error.ShouldBeNull();
    [Fact] async Task should_have_tried_again_until_it_succeeded() => await _jobsManager.Received(3).Rehydrate();
    [Fact] void should_ensure_bootstrap_clients() => _bootstrapClientsEnsured.ShouldBeTrue();
}
