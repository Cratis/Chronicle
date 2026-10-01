// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

public class and_subscription_attempts_to_interleave_with_cleanup : for_Observer.given.an_observer
{
    ObserverRemover _remover;
    Exception _subscriptionError;

    void Establish()
    {
        var grains = Substitute.For<IGrainFactory>();
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([_observerKey.Namespace]);
        grains.GetGrain<INamespaces>(_observerKey.EventStore).Returns(namespaces);
        grains.GetGrain<IObserver>(_observerKey).Returns(_observer);
        grains.GetGrain<Cratis.Orleans.Jobs.IJobsManager>(0, new Cratis.Orleans.Jobs.JobsManagerKey(_observerKey.EventStore, _observerKey.Namespace)).Returns(_jobsManager);
        _eventStoreStorage.Observers.Has(_observerId).Returns(true);
        _eventStoreNamespaceStorage.FailedPartitions.RemoveAllFor(_observerId).Returns(async _ =>
        {
            // Crash after clears but during cleanup; the new activation must still reject subscriptions.
            await Crash();
            _subscriptionError = await Catch.Exception(() => _observer.Subscribe<ObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero));
        });
        _remover = new(grains, _storage, NullLogger<ObserverRemover>.Instance);
    }

    async Task Because() => await _remover.Remove(_observerKey.EventStore, _observerId, _observerKey.EventSequenceId);

    [Fact] void should_reject_subscription_through_the_durable_fence() => _subscriptionError.ShouldBeOfExactType<ObserverRemovalInProgress>();
}
