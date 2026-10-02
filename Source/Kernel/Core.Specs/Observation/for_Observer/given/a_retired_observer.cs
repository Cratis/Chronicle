// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_retired_observer : an_observer
{
    protected Guid _retiredLifecycle;

    async Task Establish()
    {
        await _observer.Retire();
        await Crash();
        _retiredLifecycle = _stateStorage.State.AlertLifecycleId;
        _silo.StorageManager.GetStorageStats(nameof(Storage.Observation.ObserverDefinition))!.ResetCounts();
        _eventTypesStorage.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();
    }
}
