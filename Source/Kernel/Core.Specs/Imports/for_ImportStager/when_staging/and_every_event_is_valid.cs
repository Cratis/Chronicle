// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports.for_ImportStager.when_staging;

public class and_every_event_is_valid : given.a_stager
{
    StagedImportChunk _stored;

    async Task Because()
    {
        _result = await Stage(1, Event(), Event());
        _stored = (await _imports.GetChunk(_key.ImportId, 1))!;
    }

    [Fact] void should_stage_every_event() => _result.EventsStaged.ShouldEqual(2);
    [Fact] void should_report_it_was_not_staged_before() => _result.AlreadyStaged.ShouldBeFalse();
    [Fact] void should_number_the_events_in_the_import() => _stored.Events.Select(_ => _.Index).ShouldEqual([100L, 101L]);
    [Fact] void should_keep_when_the_event_occurred() => _stored.Events.First().Occurred.ShouldEqual(DateTimeOffset.UnixEpoch);
    [Fact] void should_keep_the_source_sequence_number() => _stored.Events.First().SourceSequenceNumber.ShouldEqual(42UL);
    [Fact] void should_keep_the_content_as_received() => _stored.Events.First().Content.ShouldEqual("""{"name":"Alice"}""");
    [Fact] async Task should_tell_the_import() => await _import.Received(1).ChunkStaged(Arg.Is<ImportChunkSummary>(_ => _.ChunkIndex == 1 && _.EventsStaged == 2));
}
