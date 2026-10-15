// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriptions.when_recovering;

public class and_one_namespace_fails : given.a_capture_events_subscriptions
{
    Exception _exception;

    void Establish()
    {
        var failing = Substitute.For<IObserver>();
        AnyNeedsRecovery(failing).Returns<Task<bool>>(_ => throw new TimeoutException());
        _observersByNamespace["first"] = failing;
        var stalled = Substitute.For<IObserver>();
        AnyNeedsRecovery(stalled).Returns(true);
        _observersByNamespace["second"] = stalled;
    }

    async Task Because() => _exception = await Catch.Exception(() => _subscriptions.Recover(_eventStore, _definition));

    [Fact] void should_surface_the_failure() => _exception.ShouldBeOfExactType<AggregateException>();
    [Fact] void should_still_recover_the_other_namespace() =>
        AnyRecover(_observersByNamespace["second"].Received(1));
}
