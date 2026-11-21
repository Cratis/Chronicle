// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_an_application_observer_cannot_activate : given.a_startup_task_with_persisted_capture
{
    readonly TimeoutException _activationFailure = new();
    Exception _error;

    void Establish() => _reactorObserver.Ensure().Returns(_ => Task.FromException(_activationFailure));

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_not_fail_startup() => _error.ShouldBeNull();
    [Fact] async Task should_retry_in_the_background_once_per_pass() => await _reactorObserver.Received(6).Ensure();
    [Fact] void should_complete_startup() => _bootstrapClientsEnsured.ShouldBeTrue();
    [Fact] void should_report_giving_up() => LogEntries.Any(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.DeferredStartupWorkGaveUp)).ShouldBeTrue();
}
