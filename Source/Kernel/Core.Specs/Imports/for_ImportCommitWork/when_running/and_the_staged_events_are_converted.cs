// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.when_running;

public class and_the_staged_events_are_converted : given.an_import_being_committed
{
    readonly List<ImportEventToAppend> _appended = [];

    void Establish() => _sequence.AppendImportChunk(_key.ImportId, Arg.Any<int>(), Arg.Any<EventSequenceNumber>(), Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>())
        .Returns(call =>
        {
            _appended.AddRange(call.ArgAt<IReadOnlyList<ImportEventToAppend>>(4));
            return Appended(call.ArgAt<EventSequenceNumber>(2), call.ArgAt<IReadOnlyList<ImportEventToAppend>>(4));
        });

    async Task Because() => await _work.Run(CancellationToken.None);

    Causation ImportCausationOf(ImportEventToAppend @event) => @event.Event.Causation!.First();

    [Fact] void should_keep_when_each_event_occurred() => _appended[3].Event.Occurred.ShouldEqual(DateTimeOffset.UnixEpoch.AddDays(3));
    [Fact] void should_record_the_import_at_the_root_of_the_causation() => _appended.ShouldEachConformTo(_ => ImportCausationOf(_).Type == CausationType.Import);
    [Fact] void should_record_the_import() => ImportCausationOf(_appended[0]).Properties["importId"].ShouldEqual(_key.ImportId.Value.ToString());
    [Fact] void should_record_the_source_sequence_number() => ImportCausationOf(_appended[2]).Properties["sourceSequenceNumber"].ShouldEqual("102");
    [Fact] void should_record_the_chunk() => ImportCausationOf(_appended[2]).Properties["chunkIndex"].ShouldEqual("1");
    [Fact] void should_keep_the_index_of_each_event() => _appended.Select(_ => _.Index).ShouldEqual([0L, 1L, 2L, 3L]);
}
