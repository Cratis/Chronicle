// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_reloadable_state : an_observer_automatically_reconciled_during_a_probe
{
    protected readonly reloadable_observer_state_storage _reloadableStateStorage = new();

    public an_observer_with_reloadable_state()
    {
        _silo.Options.StorageFactory = type => type == typeof(ObserverState) ? _reloadableStateStorage : null!;
    }
}
