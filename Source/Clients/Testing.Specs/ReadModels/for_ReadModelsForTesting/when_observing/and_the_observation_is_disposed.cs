// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class and_the_observation_is_disposed : given.a_collection
{
    async Task Because()
    {
        _observation = _store.ReadModels.Observe<ObservedModule>();
        Subscribe();
        await WaitForEmission();
        ((IDisposable)_observation).Dispose();
        await _store.EventLog.Append(EventSourceId.New(), new ModuleCreated("Second"));
    }

    [Fact] void should_not_emit_again() => _emissions.Count.ShouldEqual(1);
}
