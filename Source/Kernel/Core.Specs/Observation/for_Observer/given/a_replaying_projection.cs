// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_replaying_projection : an_observer
{
    protected JobId _replayJob;

    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _stateStorage.State = _stateStorage.State with
        {
            IsReplaying = true,
            HandledEventCount = 42,
            HandledEventCountPerEventType = ImmutableDictionary<EventTypeId, EventCount>.Empty.Add("event", 42)
        };
        _replayJob = JobId.New();
        _jobsManager.Resume(_replayJob).Returns(true);
        _jobsManager.GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(new JobState
            {
                Id = _replayJob,
                Status = JobStatus.Stopped,
                Request = new ReplayObserverRequest(_observerKey, ObserverType.Projection, [])
            })));
    }
}
