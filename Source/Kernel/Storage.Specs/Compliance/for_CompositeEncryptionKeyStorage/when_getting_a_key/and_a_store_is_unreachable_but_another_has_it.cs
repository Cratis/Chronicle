// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_a_key;

public class and_a_store_is_unreachable_but_another_has_it : given.two_key_stores
{
    EncryptionKey _key;
    EncryptionKey? _result;
    Exception _error;

    async Task Establish()
    {
        _key = KeyNamed("reachable");
        await Save(_secondary, _key);
        _composite = new CompositeEncryptionKeyStorage(AnUnreachableStore(), _secondary);
    }

    async Task Because() => _error = await Catch.Exception(async () => _result = await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));

    [Fact] void should_not_release_a_key_without_every_fence() => _result.ShouldBeNull();
    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
}
