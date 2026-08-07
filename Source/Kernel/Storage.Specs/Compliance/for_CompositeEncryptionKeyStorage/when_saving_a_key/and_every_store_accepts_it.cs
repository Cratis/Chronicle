// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_saving_a_key;

public class and_every_store_accepts_it : given.two_key_stores
{
    EncryptionKey _key;
    EncryptionKey? _inPrimary;
    EncryptionKey? _inSecondary;

    void Establish() => _key = KeyNamed("saved");

    async Task Because()
    {
        await _composite.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _key);
        _inPrimary = await KeyIn(_primary);
        _inSecondary = await KeyIn(_secondary);
    }

    [Fact] void should_save_it_in_the_primary_store() => _inPrimary.ShouldEqual(_key);
    [Fact] void should_save_it_in_the_secondary_store() => _inSecondary.ShouldEqual(_key);
}
