// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_a_key;

public class and_only_the_secondary_store_has_it : given.two_key_stores
{
    EncryptionKey _key;
    EncryptionKey? _result;
    EncryptionKey? _healedIntoPrimary;
    int _primaryRevisions;
    int _secondaryRevisions;

    async Task Establish()
    {
        _key = KeyNamed("secondary");
        await Save(_secondary, _key);
    }

    async Task Because()
    {
        _result = await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _healedIntoPrimary = await KeyIn(_primary);
        _primaryRevisions = await RevisionCountIn(_primary);
        _secondaryRevisions = await RevisionCountIn(_secondary);
    }

    [Fact] void should_return_the_key() => _result.ShouldEqual(_key);
    [Fact] void should_heal_the_key_into_the_primary_store() => _healedIntoPrimary.ShouldEqual(_key);
    [Fact] void should_heal_it_as_the_only_revision_in_the_primary_store() => _primaryRevisions.ShouldEqual(1);
    [Fact] void should_leave_the_secondary_store_with_its_single_revision() => _secondaryRevisions.ShouldEqual(1);
}
