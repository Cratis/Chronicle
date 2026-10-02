// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriptions.when_a_namespace_is_added;

public class and_every_attempt_times_out : given.a_namespace_subscription
{
    Exception _error;

    void Establish() => _capture.Subscribe(_added.EventStore, _added.Namespace)
        .Returns(_ => Task.FromException(new TimeoutException()));

    async Task Because()
    {
        await _onNamespaceAdded(_added);
        _error = await Catch.Exception(() => _silo.TimerRegistry.FireAllAsync());
    }

    [Fact] void should_leave_retries_to_reconciliation() => _capture.Received(1).Subscribe(_added.EventStore, _added.Namespace);
    [Fact] void should_dispose_the_notification_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] void should_not_propagate_the_failure_to_the_channel() => _error.ShouldBeNull();
}
