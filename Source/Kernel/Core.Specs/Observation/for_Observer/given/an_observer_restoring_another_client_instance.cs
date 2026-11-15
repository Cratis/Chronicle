// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// An observer observing for one instance of a client, whose keep-alive restores another instance of the same client,
/// with storage and collaborator calls counted from here on.
/// </summary>
public class an_observer_restoring_another_client_instance : an_observer_served_by_one_client_instance
{
    protected INamespaceStorage _namespaces;

    void Establish()
    {
        _namespaces = Substitute.For<INamespaceStorage>();
        _namespaces.GetAll().Returns([new NamespaceState(_observerKey.Namespace, DateTimeOffset.UtcNow)]);
        _eventStoreStorage.Namespaces.Returns(_namespaces);
    }

    protected Task RestoreSecondClient(bool isReplayable = true, EventType[]? eventTypes = null) =>
        _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, eventTypes ?? [_eventType], _secondSilo, _secondClient, isReplayable);
}
