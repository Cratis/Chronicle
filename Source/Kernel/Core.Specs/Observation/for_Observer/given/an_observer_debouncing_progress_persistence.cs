// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_debouncing_progress_persistence : an_observer_with_subscription_for_specific_event_type
{
    protected const int PersistenceInterval = 3;

    protected override Observers CreateObserversConfig() => new() { StatePersistenceBatchInterval = PersistenceInterval };
}
