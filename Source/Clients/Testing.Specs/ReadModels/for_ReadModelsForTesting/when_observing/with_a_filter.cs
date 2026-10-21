// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class with_a_filter : given.a_collection
{
    async Task Because()
    {
        await _store.EventLog.Append(EventSourceId.New(), new ModuleCreated("Second"));
        _observation = _store.ReadModels.Observe<ObservedModule>(row => row.Name == "Second");
        Subscribe();
        await WaitForEmission();
    }

    [Fact] void should_emit_only_matching_instances() => _emissions.Single().Select(row => row.Name).ShouldContainOnly("Second");
}
