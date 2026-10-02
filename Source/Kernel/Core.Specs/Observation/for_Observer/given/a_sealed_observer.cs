// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_sealed_observer : an_observer
{
    async Task Establish()
    {
        await _observer.Remove();
        _storageStats.ResetCounts();
        _silo.StorageManager.GetStorageStats(nameof(ObserverDefinition))!.ResetCounts();
        _eventTypesStorage.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();
    }
}
