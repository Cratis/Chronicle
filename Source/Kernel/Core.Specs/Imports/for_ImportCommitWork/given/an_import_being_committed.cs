// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.Imports;
using Cratis.Chronicle.Storage.InMemory.Imports;

namespace Cratis.Chronicle.Imports.for_ImportCommitWork.given;

public class an_import_being_committed : Specification
{
    protected ImportKey _key;
    protected IImport _import;
    protected ImportStorage _storage;
    protected IEventSequence _sequence;
    protected ImportRecord _record;
    protected List<int> _committedChunks;
    protected List<EventSequenceNumber> _committedFrom;
    protected List<bool> _validationFirsts;
    protected List<bool> _mayBeCommitted;
    protected ImportCommitWork _work;
    protected ulong _nextSequenceNumber;

    async Task Establish()
    {
        _key = new("store", "namespace", ImportId.New());
        _storage = new();
        _committedChunks = [];
        _committedFrom = [];
        _validationFirsts = [];
        _mayBeCommitted = [];
        _nextSequenceNumber = 10;
        _record = new ImportRecord(_key.ImportId, EventSequenceId.Log, OnInvalidEvent.Reject, DateTimeOffset.UtcNow)
        {
            State = ImportState.Validating,
            ExpectedChunks = 2,
            ExpectedEvents = 4,
            EventTypes = ["event-type"]
        };
        await _storage.AddChunk(Chunk(0));
        await _storage.AddChunk(Chunk(1));

        _import = Substitute.For<IImport>();
        _import.Get().Returns(_ => _record);
        _import.When(_ => _.CommitStarted(Arg.Any<EventSequenceNumber>())).Do(call =>
            _record = _record with { State = ImportState.Committing, CommitStartedAt = call.Arg<EventSequenceNumber>().Value });
        _import.When(_ => _.ChunkCommitted(Arg.Any<int>(), Arg.Any<IEnumerable<ImportEventOutcome>>())).Do(call =>
        {
            var outcomes = call.Arg<IEnumerable<ImportEventOutcome>>().ToArray();
            _committedChunks.Add(call.Arg<int>());
            _record = _record with
            {
                LastCommittedChunk = call.Arg<int>(),
                CommittedRanges = [.. _record.CommittedRanges, new ImportCommittedRange(call.Arg<int>(), outcomes.Min(_ => _.SequenceNumber.Value), outcomes.Max(_ => _.SequenceNumber.Value))]
            };
        });
        _import.When(_ => _.ValidationRejected(Arg.Any<IEnumerable<ImportRejection>>())).Do(_ => _record = _record with { State = ImportState.Failed });
        _import.When(_ => _.CommitCompleted()).Do(_ => _record = _record with { State = ImportState.Completed });
        _import.When(_ => _.Fail(Arg.Any<string>())).Do(call => _record = _record with { State = ImportState.Failed, FailureReason = call.Arg<string>() });

        _sequence = Substitute.For<IEventSequence>();
        _sequence.GetTailSequenceNumber().Returns(new EventSequenceNumber(9));
        _sequence.ValidateImportChunk(_key.ImportId, Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>()).Returns(call =>
        {
            _validationFirsts.Add(call.ArgAt<bool>(1));
            return new ImportChunkValidationResult([]);
        });
        _sequence.AppendImportChunk(_key.ImportId, Arg.Any<int>(), Arg.Any<EventSequenceNumber>(), Arg.Any<bool>(), Arg.Any<IReadOnlyList<ImportEventToAppend>>())
            .Returns(call =>
            {
                _mayBeCommitted.Add(call.ArgAt<bool>(3));
                return Appended(call.ArgAt<EventSequenceNumber>(2), call.ArgAt<IReadOnlyList<ImportEventToAppend>>(4));
            });

        _work = new(_key, _import, _storage, _ => _sequence, 1000, 3, retryDelay: _ => TimeSpan.Zero);
    }

    protected ImportChunkAppendResult Appended(EventSequenceNumber committedFrom, IReadOnlyList<ImportEventToAppend> events)
    {
        _committedFrom.Add(committedFrom);
        return new(events.Select(_ => new ImportEventOutcome(_.Index, _.Event.EventType, ImportEventOutcomeKind.Appended, _nextSequenceNumber++, [])).ToArray(), []);
    }

    protected StagedImportChunk Chunk(int index) => new(
        _key.ImportId,
        index,
        index * 2,
        2,
        [Event(index * 2), Event((index * 2) + 1)],
        []);

    protected StagedImportEvent Event(long index) => new(
        index,
        "event-type+1",
        "Default",
        $"source-{index}",
        "All",
        "Default",
        "{}",
        [],
        [],
        DateTimeOffset.UnixEpoch.AddDays(index),
        null,
        null,
        null,
        null,
        null,
        null,
        (ulong)(100 + index));
}
