// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_using_namespace_receipts;

public class and_reading_receipts : given.a_fenced_receipt_storage
{
    EffectReceipt _single;
    EffectReceipt[] _occurrence;
    EffectReceipt[] _occurrences;
    EventPublicationReceipt _publicationReceipt;
    IReadOnlyDictionary<string, StoredEventPublication> _publications;

    void Establish()
    {
        StoreReceipt();
        StorePublication();
    }

    async Task Because()
    {
        _single = await _storage.GetEffectReceipt(_context, _receipt.Id);
        _occurrence = (await _storage.GetEffectReceipts(_context, _receipt.Lease.OccurrenceId)).ToArray();
        _occurrences = (await _storage.GetEffectReceiptsForOccurrences(_context, [_receipt.Lease.OccurrenceId])).ToArray();
        (await _storage.TryGetPublication(_context, _publication)).TryGetValue(out _publicationReceipt);
        _publications = await _storage.TryGetPublications(_context, [_publication.Id]);
    }

    [Fact] void should_read_the_effect_receipt() => _single.Id.ShouldEqual(_receipt.Id);
    [Fact] void should_read_occurrence_receipts() => _occurrence.Single().Id.ShouldEqual(_receipt.Id);
    [Fact] void should_read_receipts_across_occurrences() => _occurrences.Single().Id.ShouldEqual(_receipt.Id);
    [Fact] void should_read_the_publication() => _publicationReceipt.SequenceNumber.Value.ShouldEqual(99UL);
    [Fact] void should_read_publication_identities() => _publications[_publication.Id].Fingerprint.ShouldEqual(_publication.Fingerprint);
    [Fact] void should_commit_each_read_fence() => _commits.ShouldEqual(5);
    [Fact] void should_use_the_fenced_receipt_sessions() => _receiptReadSessions.SequenceEqual(_sessions.Take(3)).ShouldBeTrue();
    [Fact] void should_use_the_fenced_publication_sessions() => _lookupSessions.SequenceEqual(_sessions.Skip(3)).ShouldBeTrue();
}
