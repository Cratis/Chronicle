// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.States;
using Orleans.BroadcastChannel;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.given;

public class a_manager_with_a_quarantined_observer : a_manager_recovering_a_retired_observer
{
    protected Func<NamespaceAdded, Task> _onNamespaceAdded;
    protected virtual bool AllEvents => false;

    async Task Establish()
    {
        // Use the same real observer wiring, but revive the retained retired lifecycle before quarantining it.
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Projection, [EventType.Unknown], SiloAddress.Zero);
        await _observer.TransitionTo<QuarantinedObserver>();
        var state = _managerSilo.StorageManager.GetStorage<ProjectionsManagerState>(typeof(ProjectionsManager).FullName);
        _projectionDefinition = _projectionDefinition with { SubscribesToAllEvents = AllEvents };
        state.State.Projections = [_projectionDefinition];
        _managedObserver.ReconcileSubscription<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<bool>())
            .Returns(call => _observer.ReconcileSubscription<IProjectionObserverSubscriber>(ObserverType.Projection, call.Arg<IEnumerable<EventType>>(), SiloAddress.Zero, call.Arg<bool>()));
        var channel = Substitute.For<IBroadcastChannelSubscription>();
        channel.ChannelId.Returns(ChannelId.Create("namespaces", _observerKey.EventStore.Value));
        channel.When(_ => _.Attach(Arg.Any<Func<NamespaceAdded, Task>>(), Arg.Any<Func<Exception, Task>>()))
            .Do(call => _onNamespaceAdded = call.Arg<Func<NamespaceAdded, Task>>());
        await _manager.OnSubscribed(channel);
        _managedObserver.ClearReceivedCalls();
    }
}
