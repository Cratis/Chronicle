// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Applications.Orleans.StateMachines;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Cratis.Chronicle.Grains.Observation.States.for_Replay.given;

public class a_replay_state : Specification
{
    protected Mock<IObserver> observer;
    protected ObserverKey observer_key;
    protected Mock<IObserverServiceClient> observer_service_client;
    protected Mock<IJobsManager> jobs_manager;
    protected ObserverSubscription subscription;
    protected ObserverState stored_state;
    protected ObserverState resulting_stored_state;
    protected Replay state;
    protected ObserverId observer_id;

    void Establish()
    {
        observer = new();
        observer_id = Guid.NewGuid().ToString();
        observer_key = new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        observer_service_client = new();
        jobs_manager = new();
        state = new Replay(
            observer_id,
            observer_key,
            observer_service_client.Object,
            jobs_manager.Object,
            Mock.Of<ILogger<Replay>>());
        state.SetStateMachine(observer.Object);

        stored_state = new ObserverState
        {
            ObserverId = observer_id,
            RunningState = ObserverRunningState.CatchingUp,
        };

        subscription = new ObserverSubscription(
            stored_state.ObserverId,
            new(stored_state.ObserverId, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Log),
            [],
            typeof(object),
            SiloAddress.Zero,
            string.Empty);

        observer.Setup(_ => _.GetSubscription()).Returns(() => Task.FromResult(subscription));

        jobs_manager
            .Setup(_ => _.GetJobsOfType<IReplayObserver, ReplayObserverRequest>())
            .ReturnsAsync(Enumerable.Empty<JobState>().ToImmutableList());
    }
}
