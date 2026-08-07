// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_a_key;

public class and_only_the_primary_store_has_it : given.two_key_stores
{
    EncryptionKey _key;
    EncryptionKey? _result;
    bool _secondaryHasIt;

    async Task Establish()
    {
        _key = KeyNamed("primary");
        await Save(_primary, _key);
    }

    async Task Because()
    {
        _result = await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _secondaryHasIt = await HasKeyIn(_secondary);
    }

    [Fact] void should_return_the_key() => _result.ShouldEqual(_key);
    [Fact] void should_not_write_the_key_backwards_into_the_secondary_store() => _secondaryHasIt.ShouldBeFalse();
}
