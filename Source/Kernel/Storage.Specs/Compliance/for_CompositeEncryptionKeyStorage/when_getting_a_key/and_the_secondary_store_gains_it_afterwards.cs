// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_a_key;

public class and_the_secondary_store_gains_it_afterwards : given.two_key_stores
{
    EncryptionKey _key;
    EncryptionKey? _before;
    EncryptionKey? _after;
    EncryptionKey? _healedIntoPrimary;

    void Establish() => _key = KeyNamed("appears-later");

    async Task Because()
    {
        _before = await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await Save(_secondary, _key);
        _after = await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _healedIntoPrimary = await KeyIn(_primary);
    }

    [Fact] void should_report_the_key_as_absent_before_it_exists() => _before.ShouldBeNull();
    [Fact] void should_return_the_key_once_it_exists() => _after.ShouldEqual(_key);
    [Fact] void should_heal_the_key_into_the_primary_store() => _healedIntoPrimary.ShouldEqual(_key);
}
