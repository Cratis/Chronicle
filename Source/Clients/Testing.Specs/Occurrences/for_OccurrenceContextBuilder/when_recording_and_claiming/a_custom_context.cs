// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Storage.Occurrences;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.Occurrences.for_OccurrenceContextBuilder.when_recording_and_claiming;

public class a_custom_context : Specification
{
    EventStoreForTesting _store;
    OccurrenceContext _context;
    OccurrenceLeaseResult _completed;

    void Establish() => _store = new(null, new InMemoryClientArtifactsProvider());

    async Task Because()
    {
        _context = await new OccurrenceContextBuilder().For<ExampleTrigger>().WithId(new("custom-id")).Kind(Chronicle.Occurrences.OccurrenceKind.Schedule).DueAt(_store.Time.Now).RecordedAt(_store.Time.Now).Attempt(3).Epoch(7).RecordAndClaimIn(_store);
        var result = await ((IChronicleServicesAccessor)_store.Connection).Services.Occurrences.CompleteOccurrence(new() { EventStore = _store.Name, Namespace = _store.Namespace, Trigger = _context.Trigger, OccurrenceId = _context.Id, Epoch = _context.Lease.Epoch }).EnsureSuccess();
        _completed = result.Status;
    }

    [Fact] void should_hold_a_real_fenced_lease() => _completed.ShouldEqual(OccurrenceLeaseResult.Updated);
    [Fact] void should_use_the_selected_attempt() => _context.Attempt.ShouldEqual(3);
    [Fact] void should_use_the_selected_epoch() => _context.Lease.Epoch.ShouldEqual(7L);
    [Fact] void should_preserve_due_time() => _context.DueAt.ShouldEqual(_store.Time.Now);
    [Fact] void should_preserve_the_id() => _context.Id.Value.ShouldEqual("custom-id");

    void Destroy() => _store.Dispose();

    [Trigger("example-trigger")]
    public record ExampleTrigger;
}
