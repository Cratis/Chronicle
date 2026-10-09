// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriptions.when_recovering;

public class in_a_namespace_that_needs_it : given.a_capture_events_subscriptions
{
    void Establish()
    {
        var healthy = Substitute.For<IObserver>();
        AnyNeedsRecovery(healthy).Returns(false);
        _observersByNamespace["first"] = healthy;
        var stalled = Substitute.For<IObserver>();
        AnyNeedsRecovery(stalled).Returns(true);
        _observersByNamespace["second"] = stalled;
    }

    async Task Because() => await _subscriptions.Recover(_eventStore, _definition);

    [Fact] void should_recover_the_stalled_namespace() =>
        AnyRecover(_observersByNamespace["second"].Received(1));
    [Fact] void should_leave_the_healthy_namespace_alone() =>
        AnyRecover(_observersByNamespace["first"].DidNotReceive());
}
