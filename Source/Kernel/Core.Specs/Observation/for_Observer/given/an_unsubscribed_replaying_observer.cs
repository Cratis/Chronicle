// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_unsubscribed_replaying_observer : an_observer
{
    protected EventType[] _definedEventTypes;

    async Task Establish()
    {
        _definedEventTypes = [new EventType("original-event", EventTypeGeneration.First)];
        _definitionStorage.State = _definitionStorage.State with
        {
            Type = ObserverType.Reactor,
            EventTypes = _definedEventTypes
        };
        await _definitionStorage.WriteStateAsync();
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };
        await _stateStorage.WriteStateAsync();
        _jobsManager.GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
    }
}
