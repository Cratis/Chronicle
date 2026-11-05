// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// An observer observing for one instance of a client, with storage and collaborator calls counted from here on, so a
/// spec can tell whether another instance's registration ran the full subscribe or was handed over in memory.
/// </summary>
public class an_observer_served_by_one_client_instance : an_observer
{
    protected static readonly EventType _eventType = new("5f7d3a8e-0c1b-4a2e-9d3f-7b6c5e4a3d21", EventTypeGeneration.First);
    protected ConnectedClient _firstClient;
    protected ConnectedClient _secondClient;
    protected SiloAddress _firstSilo;
    protected SiloAddress _secondSilo;

    protected TestStorageStats _definitionStorageStats => _silo.StorageManager.GetStorageStats(nameof(ObserverDefinition))!;

    async Task Establish()
    {
        _firstClient = new ConnectedClient { ConnectionId = "a-connection", Version = "1.0.0" };
        _secondClient = new ConnectedClient { ConnectionId = "b-connection", Version = "1.0.0" };
        _firstSilo = SiloAddress.FromParsableString("127.0.0.1:11111@1");
        _secondSilo = SiloAddress.FromParsableString("127.0.0.1:11112@1");
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], _firstSilo, _firstClient);

        _storageStats.ResetCounts();
        _definitionStorageStats.ResetCounts();
        _failedPartitionsStorageStats.ResetCounts();
        _jobsManager.ClearReceivedCalls();
        _eventTypesStorage.ClearReceivedCalls();
    }

    protected Task SubscribeSecondClient(bool isReplayable = true) =>
        _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], _secondSilo, _secondClient, isReplayable);
}
