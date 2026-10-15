// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

public class and_the_mapping_changes_and_replays : given.a_sink
{
    ReplayContext _replay;
    ExpandoObject? _stateDuringReplayBeforeFolding;
    ExpandoObject? _stateAfterReplay;

    async Task Because()
    {
        await _sink.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5);

        _replay = new(new ReadModelType("totals", ReadModelGeneration.First), "container", "revert", new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
        await _sink.BeginReplay(_replay);
        _stateDuringReplayBeforeFolding = await _sink.FindOrDefault("customer-1");

        // The same source position is folded again, under a changed mapping and therefore different content.
        await _sink.ApplyChanges("customer-1", Fold(new(), 11, 1, 5), 5);
        await _sink.ApplyChanges("customer-1", Fold(new(), 11, 1, 5), 5);
        await _sink.EndReplay(_replay);
        _stateAfterReplay = await _sink.FindOrDefault("customer-1");
    }

    [Fact] void should_start_the_replay_from_empty_state() => _stateDuringReplayBeforeFolding.ShouldBeNull();
    [Fact] void should_append_a_new_public_instance_without_rewriting_history() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).Count.ShouldEqual(2);
    [Fact] void should_keep_the_original_instance_unchanged() => TotalOf(_destinations.Events(Store, Tenant, EventSequenceId.Outbox)[0].Content).ShouldEqual("10");
    [Fact] void should_not_duplicate_a_retry_within_the_replay_occurrence() => _destinations.AppendAttempts.ShouldEqual(3);
    [Fact] void should_continue_from_the_replayed_state() => TotalOf(_stateAfterReplay).ShouldEqual("11");
}
