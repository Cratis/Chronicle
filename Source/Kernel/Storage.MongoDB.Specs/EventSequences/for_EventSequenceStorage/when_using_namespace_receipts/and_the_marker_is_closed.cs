// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_using_namespace_receipts;

public class and_the_marker_is_closed : given.a_fenced_receipt_storage
{
    Exception[] _failures;

    void Establish()
    {
        _matches = 0;
        StoreReceipt();
        StorePublication();
    }

    async Task Because()
    {
        _failures =
        [
            await Catch.Exception(() => _storage.AppendManyWithEffectReceipt(_context, [], _receipt)),
            await Catch.Exception(() => _storage.AppendPublication(_context, _publication, _event)),
            await Catch.Exception(() => _storage.GetEffectReceipt(_context, _receipt.Id)),
            await Catch.Exception(() => _storage.GetEffectReceipts(_context, _receipt.Lease.OccurrenceId)),
            await Catch.Exception(() => _storage.GetEffectReceiptsForOccurrences(_context, [_receipt.Lease.OccurrenceId])),
            await Catch.Exception(() => _storage.TryGetPublication(_context, _publication)),
            await Catch.Exception(() => _storage.TryGetPublications(_context, [_publication.Id]))
        ];
    }

    [Fact] void should_reject_every_receipt_entry_point() => _failures.All(error => error is NamespaceCommitRejected).ShouldBeTrue();
    [Fact] void should_not_read_effect_receipts() => _receiptReadSessions.ShouldBeEmpty();
    [Fact] void should_not_read_publications() => _lookupSessions.ShouldBeEmpty();
    [Fact] void should_not_write_receipts() => _receiptWriteSessions.ShouldBeEmpty();
    [Fact] void should_not_write_events() => _writeSessions.ShouldBeEmpty();
    [Fact] void should_not_commit() => _commits.ShouldEqual(0);
}
