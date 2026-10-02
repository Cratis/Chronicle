// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_canceling_removal;

public class and_removal_has_completed : given.an_observer
{
    Exception _subscriptionError;

    async Task Establish()
    {
        await _observer.Remove();
        await _observer.CompleteRemoval();
        _storageStats.ResetCounts();
    }

    async Task Because()
    {
        await _observer.CancelRemoval();
        _subscriptionError = await Catch.Exception(() => _observer.Subscribe<ObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero));
    }

    [Fact] void should_not_recreate_the_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_keep_the_activation_fenced() => _subscriptionError.ShouldBeOfExactType<ObserverRemovalInProgress>();
}
