// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Orleans.StateMachines;
using Cratis.Chronicle.Storage.Observation;
using Cratis.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.States.for_CatchUp.given;

public class a_catch_up_state : Specification
{
    protected Mock<IObserver> observer;
    protected ObserverKey observer_key;
    protected Mock<IJobsManager> jobs_manager;
    protected ObserverSubscription subscription;
    protected ObserverState stored_state;
    protected ObserverState resulting_stored_state;
    protected CatchUp state;
    protected ObserverId observer_id;

    void Establish()
    {
        observer = new();
        observer_id = Guid.NewGuid();
        observer_key = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        jobs_manager = new();
        state = new CatchUp(observer_id, observer_key, jobs_manager.Object, Mock.Of<ILogger<CatchUp>>());
        state.SetStateMachine(observer.Object);

        stored_state = new ObserverState
        {
            ObserverId = Guid.NewGuid(),
            RunningState = ObserverRunningState.CatchingUp,
        };

        subscription = new ObserverSubscription(
            stored_state.ObserverId,
            new(MicroserviceId.Unspecified, TenantId.Development, EventSequenceId.Log),
            Enumerable.Empty<EventType>(),
            typeof(object),
            string.Empty);

        observer.Setup(_ => _.GetSubscription()).Returns(() => Task.FromResult(subscription));
    }
}
