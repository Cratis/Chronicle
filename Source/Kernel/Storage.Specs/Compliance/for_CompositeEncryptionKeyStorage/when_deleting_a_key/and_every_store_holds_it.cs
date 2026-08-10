// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_deleting_a_key;

public class and_every_store_holds_it : given.two_key_stores
{
    bool _primaryHasIt;
    bool _secondaryHasIt;
    EncryptionKey? _throughTheComposite;

    async Task Establish()
    {
        await Save(_primary, KeyNamed("primary"));
        await Save(_secondary, KeyNamed("secondary"));
    }

    async Task Because()
    {
        await _composite.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _primaryHasIt = await HasKeyIn(_primary);
        _secondaryHasIt = await HasKeyIn(_secondary);
        _throughTheComposite = await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    [Fact] void should_erase_it_from_the_primary_store() => _primaryHasIt.ShouldBeFalse();
    [Fact] void should_erase_it_from_the_secondary_store() => _secondaryHasIt.ShouldBeFalse();
    [Fact] void should_not_heal_it_back_on_a_later_read() => _throughTheComposite.ShouldBeNull();
}
