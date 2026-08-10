// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_or_adding_a_key;

public class and_the_secondary_store_already_has_a_different_one : given.two_key_stores
{
    EncryptionKey _inSecondaryBefore;
    EncryptionKey _candidate;
    EncryptionKey _result;
    EncryptionKey? _inSecondaryAfterwards;
    int _secondaryRevisions;

    async Task Establish()
    {
        _inSecondaryBefore = KeyNamed("secondary");
        _candidate = KeyNamed("candidate");
        await Save(_secondary, _inSecondaryBefore);
    }

    async Task Because()
    {
        _result = await _composite.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _candidate);
        _inSecondaryAfterwards = await KeyIn(_secondary);
        _secondaryRevisions = await RevisionCountIn(_secondary);
    }

    [Fact] void should_return_the_key_provisioned_on_the_primary_store() => _result.ShouldEqual(_candidate);
    [Fact] void should_not_overwrite_the_key_the_secondary_store_holds() => _inSecondaryAfterwards.ShouldEqual(_inSecondaryBefore);
    [Fact] void should_not_mint_an_additional_revision_in_the_secondary_store() => _secondaryRevisions.ShouldEqual(1);
}
