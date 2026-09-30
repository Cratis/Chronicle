// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances.given;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_observing_instances;

/// <summary>
/// A replay is promoted after a subscriber has received the first page. The replay rebuilds its own collection, so
/// the subscriber keeps the first page while it runs - not the partial state it is rebuilding - and is handed the
/// replayed state once the replay ends and promotes it.
/// </summary>
/// <remarks>
/// The in-memory sink notifies its observers synchronously, so every emission has happened by the time the replay
/// has ended.
/// </remarks>
public class and_a_replay_is_promoted_after_subscribing : a_populated_sink<InMemorySinkHarness>
{
    readonly List<string[]> _emissions = [];

    async Task Because()
    {
        using var subscription = _sink.ObserveInstances(take: 10).Subscribe(Received);

        var context = new ReplayContext(
            new ReadModelType("test-read-model", ReadModelGeneration.First),
            "paged_read_models",
            "paged_read_models-revert",
            DateTimeOffset.UtcNow);
        await _sink.BeginReplay(context);
        await Write("a", "replayed-a");
        await Write("b", "replayed-b");
        await _sink.EndReplay(context);
    }

    [Fact] void should_emit_the_first_page_and_then_the_replayed_state() => _emissions.Count.ShouldEqual(2);
    [Fact] void should_emit_the_first_page() => _emissions[0].ShouldEqual(["a", "b", "c", "d", "e"]);
    [Fact] void should_emit_the_replayed_state_once_promoted() => _emissions[^1].ShouldEqual(["replayed-a", "replayed-b"]);

    void Received(IEnumerable<ExpandoObject> instances) => _emissions.Add(Names(instances));
}
