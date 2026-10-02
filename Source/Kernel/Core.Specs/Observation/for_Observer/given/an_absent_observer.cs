// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_absent_observer : an_observer
{
    void Establish()
    {
        // A shared definition does not prove this observer ever existed in the probed namespace.
        // The grain state provider fills in the identifier even though storage has no record.
        _stateStorage.State = ObserverState.Empty with { Identifier = _observerId };
        _eventStoreNamespaceStorage.Observers.Get(_observerId).Returns(ObserverState.Empty);
        _observerAlerts.ClearReceivedCalls();
    }
}
