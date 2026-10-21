// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class and_an_event_is_appended : given.a_collection
{
    async Task Because()
    {
        _observation = _store.ReadModels.Observe<ObservedModule>();
        Subscribe();
        await WaitForEmission();
        await _store.EventLog.Append(EventSourceId.New(), new ModuleCreated("Second"));
        await WaitForEmission();
    }

    [Fact] void should_emit_again() => _emissions.Count.ShouldEqual(2);
    [Fact] void should_emit_the_full_set() => _emissions[^1].Select(row => row.Name).ShouldContainOnly("First", "Second");
}
