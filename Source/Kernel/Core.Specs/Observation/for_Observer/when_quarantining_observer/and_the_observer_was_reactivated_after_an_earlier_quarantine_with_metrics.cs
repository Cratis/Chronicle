// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_quarantining_observer;

public class and_the_observer_was_reactivated_after_an_earlier_quarantine_with_metrics : given.an_observer_with_metrics
{
    async Task Establish()
    {
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(new Observers { QuarantineOnFailedPartitionCount = 1 });
        await _observer.TransitionTo<QuarantinedObserver>();
        await Reactivate();
        await _observer.ClearObserverQuarantine();
        _observer.SetSubscription(subscription);
        await _observer.TransitionTo<Routing>();
    }

    async Task Because() => await _observer.PartitionFailed("partition-1", 42UL, ["something failed"], "stacktrace");

    [Fact] void should_count_the_new_quarantine() => _metrics.SumOf(ObserverQuarantined).ShouldEqual(2);
}
