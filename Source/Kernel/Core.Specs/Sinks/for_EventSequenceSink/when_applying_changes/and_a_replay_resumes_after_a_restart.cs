// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

/// <summary>
/// The replay context is persisted and read back after a crash. A provider may return the start time with less
/// precision, so the replay occurrence must not depend on it.
/// </summary>
public class and_a_replay_resumes_after_a_restart : given.a_sink
{
    async Task Because()
    {
        var started = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(1234567);
        var persisted = new ReplayContext(new ReadModelType("totals", ReadModelGeneration.First), "container", "container-20260201000000-abcd1234", started);
        var reloaded = persisted with { Started = started.AddTicks(-4567) };

        await _sink.BeginReplay(persisted);
        await _sink.ApplyChanges("customer-1", Fold(new(), 11, 1, 5), 5);

        // The silo crashed after the append; a new sink resumes with the context as read back from storage.
        var resumed = new EventSequenceSink(Store, Tenant, _definition, _configuration, _destinations.Grains, _destinations.Storage, _destinations.Converter);
        await resumed.ResumeReplay(reloaded);
        await resumed.ApplyChanges("customer-1", Fold(new(), 11, 1, 5), 5);
    }

    [Fact] void should_attempt_the_publication_again() => _destinations.AppendAttempts.ShouldEqual(2);
    [Fact] void should_not_publish_a_second_instance_for_the_same_step() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).Count.ShouldEqual(1);
}
