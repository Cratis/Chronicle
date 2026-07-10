// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_InMemoryEncryptionKeyStorage.when_getting_or_adding_a_key;

public class and_no_key_exists_yet : given.an_in_memory_key_store
{
    EncryptionKey _candidate;
    EncryptionKey _result;
    EncryptionKey _stored;
    int _revisionCount;

    void Establish() => _candidate = KeyNamed("candidate");

    async Task Because()
    {
        _result = await _store.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _candidate);
        _stored = (await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier))!;
        _revisionCount = await RevisionCount();
    }

    [Fact] void should_return_the_candidate_key() => _result.ShouldEqual(_candidate);
    [Fact] void should_persist_the_candidate_key() => _stored.ShouldEqual(_candidate);
    [Fact] void should_provision_exactly_one_revision() => _revisionCount.ShouldEqual(1);
}
