// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_using_namespace_receipts;

public class and_recording_no_events : given.a_fenced_receipt_storage
{
    EffectReceiptWriteResult _outcome;

    void Establish() => _receipt = _receipt with { Outcome = EffectOutcome.NoEvents, EventSequenceId = null, FirstSequenceNumber = null, LastSequenceNumber = null };
    async Task Because() => _outcome = await _storage.AppendManyWithEffectReceipt(_context, [], _receipt);

    [Fact] void should_record_the_effect() => _outcome.ShouldEqual(EffectReceiptWriteResult.Written);
    [Fact] void should_commit_the_receipt_under_the_marker_fence() => _steps.SequenceEqual(["fence", "read-receipt", "insert-receipt", "commit"]).ShouldBeTrue();
    [Fact] void should_write_no_events() => _writeSessions.ShouldBeEmpty();
}
