// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

public class when_resuming_non_projection_replays : given.a_recovered_global_replay
{
    Exception _error;

    async Task Because()
    {
        _error = await Catch.Exception(async () =>
        {
            _stateStorage.State.Request = _request with { ObserverType = ObserverType.Reactor };
            await _job.ResumeForTesting();
            _stateStorage.State.Request = _request with { ObserverType = ObserverType.Reducer };
            await _job.ResumeForTesting();
        });
    }

    [Fact] void should_not_refuse() => _error.ShouldBeNull();
    [Fact] void should_not_consult_projection_definitions() => _eventStoreStorage.Projections.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_dispatch_each_resume() => await _replayServiceClient.Received(2).ResumeReplayFor(Arg.Any<ObserverDetails>());
}
