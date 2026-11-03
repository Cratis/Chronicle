// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Regression for #4040: readiness must not wait for backlog-proportional observer activation. The silo's Active
/// stage ends once the prerequisites are done, while an observer is still activating.
/// </summary>
public class and_application_observer_activation_is_still_pending : given.a_startup_task_with_a_persisted_reactor
{
    readonly TaskCompletionSource _neverActivates = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception _error;

    void Establish() => _reactorObserver.Ensure().Returns(_ => _neverActivates.Task);

    async Task Because() => _error = await Catch.Exception(() => Execute().WaitAsync(TimeSpan.FromSeconds(5)));

    void Destroy()
    {
        _neverActivates.TrySetResult();
        Stop().GetAwaiter().GetResult();
    }

    [Fact] void should_complete_startup() => _error.ShouldBeNull();
    [Fact] void should_ensure_bootstrap_clients() => _bootstrapClientsEnsured.ShouldBeTrue();
    [Fact] void should_have_the_observer_activation_still_pending() => RecoverableWork.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_have_escalated() => _escalation.Escalations.ShouldBeEmpty();
}
