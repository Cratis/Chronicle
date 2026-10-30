// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_or_adding_a_key;

public class and_the_primary_store_is_unreachable : given.two_key_stores
{
    EncryptionKey _candidate;
    Exception _error;
    bool _secondaryHasKey;

    void Establish()
    {
        _candidate = KeyNamed("candidate");
        _composite = new CompositeEncryptionKeyStorage(AnUnreachableStore(), _secondary);
    }

    async Task Because()
    {
        // Provisioning elsewhere would let two silos mint different keys for the same subject, and every value
        // protected under the losing key would be permanently unreadable.
        _error = await Catch.Exception(async () => await _composite.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _candidate));
        _secondaryHasKey = await HasKeyIn(_secondary);
    }

    [Fact] void should_fail() => _error.ShouldBeOfExactType<StoreUnreachable>();
    [Fact] void should_not_provision_on_another_store_instead() => _secondaryHasKey.ShouldBeFalse();
}
