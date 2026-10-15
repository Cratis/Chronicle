// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class with_events_in_the_log : given.a_collection
{
    async Task Because()
    {
        _observation = _store.ReadModels.Observe<ObservedModule>();
        Subscribe();
        await WaitForEmission();
    }

    [Fact] void should_emit_the_current_set() => _emissions.Single().ShouldContainOnly(new ObservedModule(Guid.Parse(_firstId.Value), "First"));
}
