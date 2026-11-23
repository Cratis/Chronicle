// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_a_history_seed_finishes_after_a_newer_transition : given.an_observer_with_controlled_persistence
{
    readonly given.held_storage_operation<DateTimeOffset?> _history = new();
    DateTimeOffset? _observedTimestamp;

    Task Because() => InGrainTurn(PerformScenario);

    async Task PerformScenario()
    {
        _eventStoreNamespaceStorage.Observers.GetRunningStateChangedAt(_observerId).Returns(_ => _history.Invoke());
        var activating = _observer.OnActivation(CancellationToken.None);
        try
        {
            await _history.WaitForStart();
            await _observer.Unsubscribe();
        }
        finally
        {
            _history.Complete(_now.AddDays(-1));
        }
        await activating.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _observedTimestamp = (await _observer.GetInformation()).RunningStateChangedAt;
    }

    [Fact] void should_not_adopt_the_stale_seed() => _observedTimestamp.ShouldEqual(_now.AddHours(1));
    [Fact] void should_keep_the_newer_persisted_transition() => _persistedRunningStateChangedAt.ShouldEqual(_now.AddHours(1));
}
