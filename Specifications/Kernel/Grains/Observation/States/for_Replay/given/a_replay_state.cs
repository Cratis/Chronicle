// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Grains.Observation.Jobs;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Kernel.Storage.Jobs;
using Aksio.Cratis.Kernel.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Replay.given;

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
        observer_id = Guid.NewGuid();
        observer_key = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
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
            new(MicroserviceId.Unspecified, TenantId.Development, EventSequenceId.Log),
            Enumerable.Empty<EventType>(),
            typeof(object),
            string.Empty);

        observer.Setup(_ => _.GetSubscription()).Returns(() => Task.FromResult(subscription));

        jobs_manager
            .Setup(_ => _.GetJobsOfType<IReplayObserver, ReplayObserverRequest>())
            .ReturnsAsync(Enumerable.Empty<JobState>().ToImmutableList());
    }
}
