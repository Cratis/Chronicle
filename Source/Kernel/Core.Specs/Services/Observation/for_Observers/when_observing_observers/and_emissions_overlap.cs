// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_emissions_overlap : given.an_observed_state_stream
{
    readonly TaskCompletionSource<IReadOnlyDictionary<ObserverId, DateTimeOffset>> _firstQuery = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _queriesBeforeCompletion;

    void Establish() => _observerStateStorage.GetRunningStatesChangedAt().Returns(_firstQuery.Task, Task.FromResult(Timestamps(_changedTimestamp)));

    async Task Because()
    {
        await Emit(("first", ObserverRunningState.Active, 1));
        await Emit(("first", ObserverRunningState.Disconnected, 1));
        _queriesBeforeCompletion = TimestampQueries;
        _firstQuery.SetResult(Timestamps(_initialTimestamp));
        await EmittedCount(2);
    }

    [Fact] void should_emit_in_source_order() => _emitted.Select(_ => _.Single(observer => observer.Id == "first")).Select(_ => (_.RunningState, (DateTimeOffset?)_.RunningStateChangedAt)).ShouldEqual([(Contracts.Observation.ObserverRunningState.Active, (DateTimeOffset?)_initialTimestamp), (Contracts.Observation.ObserverRunningState.Disconnected, (DateTimeOffset?)_changedTimestamp)]);
    [Fact] void should_not_query_for_the_second_until_the_first_finished() => _queriesBeforeCompletion.ShouldEqual(1);
}
