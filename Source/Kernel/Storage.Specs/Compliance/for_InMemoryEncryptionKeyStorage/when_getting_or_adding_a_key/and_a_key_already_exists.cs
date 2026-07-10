// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_InMemoryEncryptionKeyStorage.when_getting_or_adding_a_key;

public class and_a_key_already_exists : given.an_in_memory_key_store
{
    EncryptionKey _existing;
    EncryptionKey _candidate;
    EncryptionKey _result;
    int _revisionCount;

    async Task Establish()
    {
        _existing = KeyNamed("existing");
        _candidate = KeyNamed("candidate");
        await _store.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _existing);
    }

    async Task Because()
    {
        _result = await _store.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _candidate);
        _revisionCount = await RevisionCount();
    }

    [Fact] void should_return_the_existing_key() => _result.ShouldEqual(_existing);
    [Fact] void should_ignore_the_candidate_key() => _result.ShouldNotEqual(_candidate);
    [Fact] void should_not_mint_an_additional_revision() => _revisionCount.ShouldEqual(1);
}
