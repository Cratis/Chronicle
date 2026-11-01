// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_definitions_change_during_timestamp_enrichment : given.an_observed_state_stream
{
    readonly TaskCompletionSource<IReadOnlyDictionary<ObserverId, DateTimeOffset>> _timestamps = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _observerStateStorage.GetRunningStatesChangedAt().Returns(_timestamps.Task);

    async Task Because()
    {
        await Emit(("third", ObserverRunningState.Active, 1));
        EmitDefinitions("third");
        _timestamps.SetResult(Timestamps(_initialTimestamp));
        await EmittedCount(1);
        await Emit(("third", ObserverRunningState.Active, 2));
        await EmittedCount(2);
    }

    [Fact] void should_use_the_latest_definitions() => _emitted[0].Single().Id.ShouldEqual("third");
    [Fact] void should_keep_state_order() => _emitted.Select(_ => _.Single().LastHandledEventSequenceNumber).ShouldEqual([1UL, 2UL]);
}
