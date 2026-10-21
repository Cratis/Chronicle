// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_ensuring_subscriptions_in_a_namespace;

public class and_quarantine_begins_after_the_managers_snapshot : given.a_manager_recovering_a_retired_observer
{
    int _snapshots;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Projection, [EventType.Unknown], SiloAddress.Zero);
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(new Observers { QuarantineOnFailedPartitionCount = 1 });

        // The manager's earlier subscription probe predates the subscription becoming active.
        _managedObserver.IsSubscriptionEstablished().Returns(_ => Task.FromResult(false), _ => _observer.IsSubscriptionEstablished());
        _managedObserver.GetState().Returns(async _ =>
        {
            var snapshot = await _observer.GetState();
            if (++_snapshots == 2)
            {
                // Quarantine after the final manager preflight, immediately before its serialized dispatch.
                await _observer.PartitionFailed("failed-partition", 42UL, ["failure"], "stack");
                (await _observer.IsObserverQuarantined()).ShouldBeTrue();
                await _observer.Unsubscribe();
            }

            return snapshot;
        });
        _managedObserver.ReconcileSubscription<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), false)
            .Returns(call => _observer.ReconcileSubscription<IProjectionObserverSubscriber>(ObserverType.Projection, call.Arg<IEnumerable<EventType>>(), SiloAddress.Zero, false));
        _managedObserver.ClearReceivedCalls();
    }

    Task Because() => _manager.EnsureSubscribedIn(_observerKey.Namespace);

    [Fact] async Task should_dispatch_through_the_serialized_helper() => await _managedObserver.Received(1).ReconcileSubscription<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), false);
    [Fact] async Task should_preserve_the_real_observers_quarantine() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] async Task should_not_install_a_subscription() => (await _observer.IsSubscribed()).ShouldBeFalse();
}
