// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs.given;

public class an_observer_with_client_owned_subscription : for_Observer.given.an_observer
{
    protected static readonly EventType event_type = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);
    protected ConnectedClient _connectedClient;
    protected IConnectedClients _connectedClientsGrain;

    async Task Establish()
    {
        _connectedClient = new ConnectedClient
        {
            ConnectionId = "test-connection-id",
            Version = "1.0.0"
        };

        _connectedClientsGrain = Substitute.For<IConnectedClients>();
        _silo.AddProbe<IConnectedClients>(_ => _connectedClientsGrain);

        _jobsManager
            .GetJobsOfType<IReplayObserver, ReplayObserverRequest>()
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));

        _jobsManager
            .GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>()
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));

        await _observer.Subscribe<IClientOwnedObserverSubscriber>(
            ObserverType.Reactor,
            [event_type],
            SiloAddress.Zero,
            _connectedClient);

        _storageStats.ResetCounts();
    }
}
