// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Storage.InMemory.Imports.for_ImportStorage;

public class when_saving_and_reading_an_import : given.an_import_storage
{
    ImportRecord _record;
    ImportRecord? _read;
    StagedImportChunk? _chunk;

    async Task Because()
    {
        _record = new ImportRecord(_importId, "my-sequence", OnInvalidEvent.Skip, DateTimeOffset.UnixEpoch)
        {
            State = ImportState.Committing,
            ExpectedChunks = 3,
            ExpectedEvents = 10,
            LastCommittedChunk = 1,
            CommitStartedAt = 40,
            CommittedRanges = [new ImportCommittedRange(0, 40, 44), new ImportCommittedRange(1, 46, 50)]
        };
        _record = _record.WithRejections([new ImportRejection(7, "first+1", ["unique: taken"])]);
        await _storage.Save(_record);
        await _storage.Save(_record with { EventsCommitted = 8 });
        await _storage.AddChunk(Chunk(_importId, 0, 2));
        var storage = FreshStorage();
        _read = await storage.Get(_importId);
        _chunk = await storage.GetChunk(_importId, 0);
    }

    [Fact] void should_keep_the_latest_save() => _read!.EventsCommitted.ShouldEqual(8L);
    [Fact] void should_keep_the_state() => _read!.State.ShouldEqual(ImportState.Committing);
    [Fact] void should_keep_the_event_sequence() => _read!.EventSequenceId.Value.ShouldEqual("my-sequence");
    [Fact] void should_keep_the_checkpoint() => (_read!.LastCommittedChunk, _read.CommittedFrom).ShouldEqual((1, 51UL));
    [Fact] void should_keep_the_rejections() => _read!.Rejections.Single().Reasons.Single().ShouldEqual("unique: taken");
    [Fact] void should_keep_when_staged_events_occurred() => _chunk!.Events.First().Occurred.ShouldEqual(DateTimeOffset.UnixEpoch);
    [Fact] void should_keep_the_causation_of_staged_events() => _chunk!.Events.First().Causation!.Single().Properties["source"].ShouldEqual("legacy");
    [Fact] void should_keep_who_caused_staged_events() => _chunk!.Events.First().CausedBy!.Name.ShouldEqual("Migrator");
    [Fact] void should_keep_the_content_of_staged_events_exactly() => _chunk!.Events.First().Content.ShouldEqual("""{"name":"Alice \"Al\" <al@example.com>"}""");
    [Fact] async Task should_list_it() => (await FreshStorage().GetAll()).Select(_ => _.Id).ShouldContain(_importId);
}
