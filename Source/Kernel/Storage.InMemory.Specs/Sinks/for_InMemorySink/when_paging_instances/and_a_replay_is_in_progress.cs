// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances.given;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_paging_instances;

/// <summary>
/// While a replay rebuilds its own collection, a page and its count both come from the primary collection.
/// </summary>
public class and_a_replay_is_in_progress : a_populated_sink<InMemorySinkHarness>
{
    ReadModelInstances _page;

    async Task Establish()
    {
        await _sink.BeginReplay(new ReplayContext(
            new ReadModelType("test-read-model", ReadModelGeneration.First),
            "paged_read_models",
            "paged_read_models-revert",
            DateTimeOffset.UtcNow));
        await Write("replayed", "replayed");
    }

    async Task Because() => _page = await _sink.GetInstances(take: 10);

    [Fact] void should_return_the_instances_of_the_primary_collection() => Names(_page).ShouldEqual(["a", "b", "c", "d", "e"]);
    [Fact] void should_count_the_instances_of_the_primary_collection() => _page.TotalCount.ShouldEqual(5L);
}
