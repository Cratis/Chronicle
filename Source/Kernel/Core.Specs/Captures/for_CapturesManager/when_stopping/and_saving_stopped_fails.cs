// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_stopping;

public class and_saving_stopped_fails : given.a_manager_with_failing_stop
{
    async Task Because()
    {
        await FailStop();
        await RecoverAutomatically();
    }

    [Fact] void should_surface_the_stop_failure() => _error.ShouldBeSame(_failure);
    [Fact] async Task should_keep_the_persisted_status_started() => (await _captures.Get(_events.Id)).Status.ShouldEqual(CaptureStatus.Started);
    [Fact] async Task should_reopen_ensure_and_timer_recovery() => await _subscriptions.Received(2).Recover(_eventStore, Arg.Is<CaptureDefinition>(definition => definition.Id == _events.Id));
    [Fact] async Task should_reopen_namespace_recovery() => await _subscriptions.Received(1).Recover(_eventStore, "new-tenant", Arg.Is<CaptureDefinition>(definition => definition.Id == _events.Id));
}
