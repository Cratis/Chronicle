// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelsForTesting.when_observing;

public class with_multiple_projection_event_sources : given.a_collection
{
    async Task Establish()
    {
        await _store.EventLog.Append(EventSourceId.New(), new ModuleCreated("Second"));
        await _store.EventLog.Append(EventSourceId.New(), new ModuleCreated("Third"));
    }

    async Task Because()
    {
        _observation = _store.ReadModels.Observe<ObservedModule>();
        Subscribe();
        await WaitForEmission();
    }

    [Fact] void should_fold_every_event_source() => _emissions.Single().Select(row => row.Name).ShouldContainOnly("First", "Second", "Third");
    [Fact] void should_keep_distinct_keys() => _emissions.Single().Select(row => row.Id).Distinct().Count().ShouldEqual(3);
}
