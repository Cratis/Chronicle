// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// A management probe activates an observer that does not exist in the namespace. It has no alerts to report, so
/// nothing keeps it resident.
/// </summary>
public class and_no_observer_is_stored_for_the_key : given.an_idle_observer
{
    async Task Establish()
    {
        // The grain state provider fills in the identifier even though storage has no record.
        _stateStorage.State = ObserverState.Empty with { Identifier = _observerId };
        _eventStoreNamespaceStorage.Observers.Get(_observerId).Returns(ObserverState.Empty);
        await Crash();
    }

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_idlePeriod);
    }

    [Fact] void should_release_the_activation() => ReleaseRequests.ShouldEqual(1);
    [Fact] void should_not_create_the_observer() => _storageStats.Writes.ShouldEqual(0);
}
