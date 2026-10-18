// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_getting_publications;

public class and_some_are_absent : given.a_keyed_batch_storage
{
    IReadOnlyDictionary<string, StoredEventPublication> _receipts;
    async Task Establish() => (await _storage.AppendManyWithIdentities([_event with { Identity = _publication }])).IsSuccess.ShouldBeTrue();
    async Task Because() => _receipts = await _storage.TryGetPublications(["absent", _publication.Id, _publication.Id]);
    [Fact] void should_omit_missing_identities_and_coalesce_repeated_inputs() => _receipts.Count.ShouldEqual(1);
    [Fact] void should_return_the_original_fingerprint() => _receipts[_publication.Id].Fingerprint.ShouldEqual(_publication.Fingerprint);
}
