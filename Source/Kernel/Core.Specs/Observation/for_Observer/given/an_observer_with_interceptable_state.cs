// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_interceptable_state : an_observer
{
    protected readonly interceptable_observer_state_storage _interceptedState = new();

    public an_observer_with_interceptable_state()
    {
        _silo.Options.StorageFactory = type => type == typeof(ObserverState) ? _interceptedState : null!;
    }

    void Establish() => _interceptedState.Writes.Clear();
}
