// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences.Idempotency;
using Cratis.Chronicle.Imports;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_import_chunk;

public class and_a_key_is_already_durable : given.an_event_sequence_with_imports
{
    readonly List<IdempotencyKey?> _keys = [new("source-event-1"), null, null];

    void Establish()
    {
        var input = Input(ImportEvents(_keys)[0].Event) with { Identity = _keys[0]!.Identity };
        _durable[_keys[0]!.Identity] = new(5, AppendFingerprint.Compute(input, []).AsT0.Format());
        _stateStorage.State.SequenceNumber = 6;
    }

    async Task Because() => _chunkResult = await AppendChunk(_keys);

    [Fact] void should_report_it_as_already_appended() => _chunkResult.Outcomes.Single(_ => _.Index == 0).Kind.ShouldEqual(ImportEventOutcomeKind.AlreadyAppended);
    [Fact] void should_report_its_stored_sequence_number() => _chunkResult.Outcomes.Single(_ => _.Index == 0).SequenceNumber.Value.ShouldEqual(5UL);
    [Fact] void should_append_the_events_without_a_key() => _written.Count.ShouldEqual(2);
}
