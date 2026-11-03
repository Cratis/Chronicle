// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// An observer that cannot activate no longer fails startup, which on a restart met the same backlog and failed the
/// same way. It is retried in the background and, once it has failed repeatedly, escalated through the seam.
/// </summary>
public class and_an_application_observer_cannot_activate : given.a_startup_task_with_a_persisted_reactor
{
    const int FailedActivations = 6;

    int _activations;
    Exception _error;
    StartupWorkItem _observerWork;

    void Establish()
    {
        _observerWork = new("ActivateObserver", _reactorObserverKey.ToString());
        _reactorObserver.Ensure().Returns(_ => ++_activations <= FailedActivations ? Task.FromException(new TimeoutException()) : Task.CompletedTask);
    }

    async Task Because() => _error = await Catch.Exception(ExecuteAndSettle);

    [Fact] void should_not_fail_startup() => _error.ShouldBeNull();
    [Fact] async Task should_keep_retrying_until_the_observer_activates() => await _reactorObserver.Received(FailedActivations + 1).Ensure();
    [Fact] void should_escalate_once_after_the_threshold() => _escalation.Escalations.Select(_ => (_.Item, _.Attempts)).ShouldContainOnly([(_observerWork, RecoverableStartupWork.EscalateAfterFailures)]);
    [Fact] void should_report_the_recovery() => _escalation.Recoveries.ShouldContainOnly([(_observerWork, FailedActivations + 1)]);
    [Fact] void should_never_wait_longer_than_the_backoff_cap() => _escalation.Escalations.All(_ => _.NextDelay <= RecoverableStartupWork.MaxDelay).ShouldBeTrue();
    [Fact] void should_ensure_bootstrap_clients() => _bootstrapClientsEnsured.ShouldBeTrue();
}
