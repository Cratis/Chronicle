// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_deleting_a_key_on_a_peer : Specification
{
    static readonly EncryptionKeyIdentifier _identifier = "5c6cce36-d60d-46db-9db2-e820559962db";
    static readonly EncryptionKey _key = new([1], [2]);

    InMemoryEncryptionKeyStorage _actualStore;
    CacheEncryptionKeyStorage _siloA;
    CacheEncryptionKeyStorage _siloB;
    EncryptionKey? _peerBeforeEviction;
    EncryptionKey? _peerAfterEviction;

    void Establish()
    {
        // One shared backing store with two cache instances stands in for two silos, each with its own cache.
        _actualStore = new InMemoryEncryptionKeyStorage();
        _siloA = new(_actualStore);
        _siloB = new(_actualStore);
    }

    async Task Because()
    {
        await _siloA.GetOrAddFor(string.Empty, string.Empty, _identifier, _key);

        // Warm silo B's cache with the key.
        await _siloB.TryGetFor(string.Empty, string.Empty, _identifier);

        // Crypto-shred through silo A: the backing store is cleared and silo A evicts its own cache.
        await _siloA.DeleteFor(string.Empty, string.Empty, _identifier);

        // Until the fan-out reaches silo B, it keeps serving the stale cached key.
        _peerBeforeEviction = await _siloB.TryGetFor(string.Empty, string.Empty, _identifier);

        // The grain-service fan-out reaches silo B, which evicts its own cache.
        _siloB.EvictFromCache(string.Empty, string.Empty, _identifier);
        _peerAfterEviction = await _siloB.TryGetFor(string.Empty, string.Empty, _identifier);
    }

    [Fact] void should_keep_serving_the_stale_key_before_eviction() => _peerBeforeEviction.ShouldNotBeNull();
    [Fact] void should_return_no_key_after_eviction() => _peerAfterEviction.ShouldBeNull();
}
