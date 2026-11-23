// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_a_history_reload_finishes_after_disposal : given.an_observer_with_controlled_persistence
{
    readonly given.held_storage_operation<DateTimeOffset?> _history = new();
    DateTimeOffset? _observedTimestamp;

    async Task Because()
    {
        _eventStoreNamespaceStorage.Observers.GetRunningStateChangedAt(_observerId).Returns(_ => _history.Invoke());
        var subscribing = Subscribe();
        try
        {
            await _history.WaitForStart();
            _observer.Dispose();
        }
        finally
        {
            _history.Complete(_now.AddDays(-1));
        }
        await subscribing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _observedTimestamp = (await _observer.GetInformation()).RunningStateChangedAt;
    }

    [Fact] void should_keep_the_existing_information_timestamp() => _observedTimestamp.ShouldEqual(_now);
    [Fact] void should_not_save_the_stale_timestamp() => ShouldNotSaveTimestamp();
}
