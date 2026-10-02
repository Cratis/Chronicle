// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriptions.when_a_namespace_is_added;

public class and_the_failure_is_not_transient : given.a_namespace_subscription
{
    Exception _error;

    void Establish() => _capture.Subscribe(_added.EventStore, _added.Namespace)
        .Returns(_ => Task.FromException(new InvalidOperationException()));

    async Task Because()
    {
        await _onNamespaceAdded(_added);
        _error = await Catch.Exception(() => _silo.TimerRegistry.FireAllAsync());
    }

    [Fact] void should_not_retry() => _capture.Received(1).Subscribe(_added.EventStore, _added.Namespace);
    [Fact] void should_not_propagate_the_failure_to_the_channel() => _error.ShouldBeNull();
}
