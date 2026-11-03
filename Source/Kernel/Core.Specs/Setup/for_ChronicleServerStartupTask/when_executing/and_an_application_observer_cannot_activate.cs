// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_an_application_observer_cannot_activate : given.a_startup_task_with_persisted_capture
{
    readonly TimeoutException _activationFailure = new();
    Exception _error;

    void Establish() => _reactorObserver.Ensure().Returns(_ => Task.FromException(_activationFailure));

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_fail_startup_with_the_application_observer_failure() => _error.ShouldEqual(_activationFailure);
    [Fact] async Task should_exhaust_the_required_rehydration_retry_budget() => await _reactorObserver.Received(5).Ensure();
    [Fact] void should_not_run_the_final_authentication_step() => _bootstrapClientsEnsured.ShouldBeFalse();
}
