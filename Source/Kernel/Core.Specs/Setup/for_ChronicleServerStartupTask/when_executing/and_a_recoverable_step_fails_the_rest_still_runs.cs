// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_a_recoverable_step_fails_the_rest_still_runs : given.a_startup_task_with_a_persisted_reactor
{
    int _webhookCalls;
    Exception _error;

    void Establish() => _webhooksManager.Ensure().Returns(_ => ++_webhookCalls <= 2 ? Task.FromException(new TimeoutException()) : Task.CompletedTask);

    async Task Because() => _error = await Catch.Exception(ExecuteAndSettle);

    [Fact] void should_not_fail_startup() => _error.ShouldBeNull();
    [Fact] async Task should_retry_the_failed_step_until_it_succeeds() => await _webhooksManager.Received(3).Ensure();
    [Fact] async Task should_still_ensure_captures() => await _capturesManager.Received(1).Ensure();
    [Fact] async Task should_still_ensure_event_store_subscriptions() => await _eventStoreSubscriptionsManager.Received(1).Ensure();
    [Fact] async Task should_still_rehydrate_jobs() => await _jobsManager.Received(1).Rehydrate();
    [Fact] async Task should_still_activate_the_application_observer() => await _reactorObserver.Received(1).Ensure();
}
