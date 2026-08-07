// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_or_adding_a_key;

public class and_the_primary_store_already_has_one : given.two_key_stores
{
    EncryptionKey _existing;
    EncryptionKey _candidate;
    EncryptionKey _result;
    EncryptionKey? _inSecondary;
    int _primaryRevisions;

    async Task Establish()
    {
        _existing = KeyNamed("existing");
        _candidate = KeyNamed("candidate");
        await Save(_primary, _existing);
    }

    async Task Because()
    {
        _result = await _composite.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _candidate);
        _inSecondary = await KeyIn(_secondary);
        _primaryRevisions = await RevisionCountIn(_primary);
    }

    [Fact] void should_return_the_existing_key() => _result.ShouldEqual(_existing);
    [Fact] void should_not_use_the_candidate_key() => _result.ShouldNotEqual(_candidate);
    [Fact] void should_mirror_the_existing_key_to_the_secondary_store() => _inSecondary.ShouldEqual(_existing);
    [Fact] void should_not_mint_an_additional_revision_on_the_primary_store() => _primaryRevisions.ShouldEqual(1);
}
