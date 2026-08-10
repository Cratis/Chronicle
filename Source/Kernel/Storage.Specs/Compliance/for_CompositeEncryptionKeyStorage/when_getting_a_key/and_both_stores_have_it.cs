// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_a_key;

public class and_both_stores_have_it : given.two_key_stores
{
    EncryptionKey _primaryKey;
    EncryptionKey _secondaryKey;
    EncryptionKey? _result;
    EncryptionKey? _secondaryAfterwards;
    int _primaryRevisions;
    int _secondaryRevisions;

    async Task Establish()
    {
        _primaryKey = KeyNamed("primary");
        _secondaryKey = KeyNamed("secondary");
        await Save(_primary, _primaryKey);
        await Save(_secondary, _secondaryKey);
    }

    async Task Because()
    {
        // Reading twice: a store that is written to on every read grows a revision every time.
        await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _result = await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _secondaryAfterwards = await KeyIn(_secondary);
        _primaryRevisions = await RevisionCountIn(_primary);
        _secondaryRevisions = await RevisionCountIn(_secondary);
    }

    [Fact] void should_return_the_key_from_the_primary_store() => _result.ShouldEqual(_primaryKey);
    [Fact] void should_leave_the_secondary_key_untouched() => _secondaryAfterwards.ShouldEqual(_secondaryKey);
    [Fact] void should_not_mint_an_additional_revision_in_the_primary_store() => _primaryRevisions.ShouldEqual(1);
    [Fact] void should_not_mint_an_additional_revision_in_the_secondary_store() => _secondaryRevisions.ShouldEqual(1);
}
