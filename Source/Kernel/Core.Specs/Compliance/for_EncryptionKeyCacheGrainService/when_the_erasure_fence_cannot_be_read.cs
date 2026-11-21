// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_EncryptionKeyCacheGrainService;

public class when_the_erasure_fence_cannot_be_read : given.a_key_cache
{
    Exception _evictionFailure;
    Exception _signingFailure;
    bool _signed;

    void Establish() => _storage.GetErasureForRelease(_address.EventStore, _address.Namespace, _address.Identifier)
        .Returns(Task.FromException<EncryptionKeyErasure?>(new InvalidOperationException("Fence unavailable")));

    async Task Because()
    {
        _evictionFailure = await Catch.Exception(() => EncryptionKeyCacheGrainService.EvictFor(_storage, _cache, _address));
        _signed = _original.TrySign("value"u8.ToArray(), out _);
        _signingFailure = Catch.Exception(() => _operation.Acquire());
    }

    [Fact] void should_propagate_the_storage_failure() => _evictionFailure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_dispose_the_cached_signer_even_on_failure() => _signed.ShouldBeFalse();
    [Fact] void should_fail_in_flight_retries_closed() => _signingFailure.ShouldBeOfExactType<CryptographicException>();
    [Fact] void should_evict_the_local_managed_key_cache() => ((IEvictEncryptionKeyCache)_storage).Received(1).EvictFromCache(_address.EventStore, _address.Namespace, _address.Identifier);
}
