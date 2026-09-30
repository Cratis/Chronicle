// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks.for_ISink.when_paging_instances.given;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_preparing_an_initial_run;

/// <summary>
/// Inside a replay, preparing an initial run empties the replay's own collection. Observers read the primary
/// collection, which is untouched, so nothing is announced to them.
/// </summary>
public class and_a_replay_is_in_progress : a_populated_sink<InMemorySinkHarness>
{
    int _emissions;
    int _emissionsAfterPreparing;

    async Task Because()
    {
        await _sink.BeginReplay(new ReplayContext(
            new ReadModelType("test-read-model", ReadModelGeneration.First),
            "paged_read_models",
            "paged_read_models-revert",
            DateTimeOffset.UtcNow));
        using var subscription = _sink.ObserveInstances(take: 10).Subscribe(_ => _emissions++);
        await _sink.PrepareInitialRun();
        _emissionsAfterPreparing = _emissions;
    }

    [Fact] void should_only_have_emitted_the_first_page() => _emissionsAfterPreparing.ShouldEqual(1);
}
