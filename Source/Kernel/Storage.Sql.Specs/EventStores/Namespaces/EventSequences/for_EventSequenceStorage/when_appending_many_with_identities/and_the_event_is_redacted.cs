// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many_with_identities;

public class and_the_event_is_redacted : given.a_keyed_batch_storage
{
    IReadOnlyDictionary<string, StoredEventPublication> _receipts;
    async Task Establish() => (await _storage.AppendManyWithIdentities([_event with { Identity = _publication }])).IsSuccess.ShouldBeTrue();
    async Task Because()
    {
        await _storage.Redact(_event.SequenceNumber, "erased", CorrelationId.New(), [], [], DateTimeOffset.UtcNow);
        _receipts = await _storage.TryGetPublications([_publication.Id]);
    }
    [Fact] void should_preserve_the_identity_and_sequence_number() => _receipts[_publication.Id].SequenceNumber.ShouldEqual(_event.SequenceNumber);
    [Fact] void should_preserve_the_fingerprint() => _receipts[_publication.Id].Fingerprint.ShouldEqual(_publication.Fingerprint);
}
