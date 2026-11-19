// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_live_observer_with_overlap_guarded_definition : a_live_observer_with_guarded_state
{
    protected overlap_guarded_storage<ObserverDefinition> _guardedDefinition;
    protected overlap_guarded_storage<ObserverState> _guardedState;

    protected static async Task WaitFor(TaskCompletionSource signal) => await signal.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

    protected override void ConfigureStorage(TestKitSilo silo, ObserverState persistedState)
    {
        base.ConfigureStorage(silo, persistedState);
        var inner = silo.Options.StorageFactory!;
        _guardedDefinition = new((IStorage<ObserverDefinition>)inner(typeof(ObserverDefinition)));
        _guardedState = new((IStorage<ObserverState>)inner(typeof(ObserverState)));
        silo.Options.StorageFactory = type =>
        {
            if (type == typeof(ObserverDefinition)) return _guardedDefinition;
            if (type == typeof(ObserverState)) return _guardedState;
            return null!;
        };
    }
}
