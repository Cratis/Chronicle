// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_persisted_pattern_capture_cannot_activate : given.a_startup_task_with_persisted_capture
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_complete_startup() => _error.ShouldBeNull();
    [Fact] async Task should_exhaust_the_capture_subscription_retry_budget() => await _patternCapture.Received(5).Subscribe(_eventStore, _namespace);
    [Fact] async Task should_not_require_capture_activation_again() => await _captureObserver.DidNotReceive().Ensure();
    [Fact] async Task should_rehydrate_the_application_observer() => await _reactorObserver.Received(1).Ensure();
    [Fact] void should_run_the_final_authentication_step() => _bootstrapClientsEnsured.ShouldBeTrue();
}
