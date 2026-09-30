// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances.given;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_preparing_an_initial_run;

/// <summary>
/// Preparing an initial run outside a replay empties the primary collection, which observers are told about.
/// </summary>
/// <remarks>
/// The in-memory sink notifies its observers synchronously, so every emission has happened by the time the call returns.
/// </remarks>
public class and_a_subscriber_observes_the_primary_collection : a_populated_sink<InMemorySinkHarness>
{
    readonly List<string[]> _emissions = [];

    async Task Because()
    {
        using var subscription = _sink.ObserveInstances(take: 10).Subscribe(instances => _emissions.Add(Names(instances)));
        await _sink.PrepareInitialRun();
    }

    [Fact] void should_emit_the_first_page_and_then_the_empty_page() => _emissions.Count.ShouldEqual(2);
    [Fact] void should_emit_an_empty_page_after_preparing() => _emissions[^1].ShouldBeEmpty();
}
