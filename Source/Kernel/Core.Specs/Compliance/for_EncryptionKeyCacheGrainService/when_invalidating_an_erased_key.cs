// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_EncryptionKeyCacheGrainService;

public class when_invalidating_an_erased_key : given.a_key_cache
{
    Exception _failure;
    bool _signed;

    void Establish() => _storage.GetErasureForRelease(_address.EventStore, _address.Namespace, _address.Identifier)
        .Returns(new EncryptionKeyErasure(8, [_key.Fingerprint], false));

    async Task Because()
    {
        await EncryptionKeyCacheGrainService.EvictFor(_storage, _cache, _address);
        _signed = _original.TrySign("value"u8.ToArray(), out _);
        _failure = Catch.Exception(() => _operation.Acquire());
    }

    [Fact] void should_dispose_the_erased_signer() => _signed.ShouldBeFalse();
    [Fact] void should_refuse_to_reacquire_for_the_stale_write() => _failure.ShouldBeOfExactType<CryptographicException>();
    [Fact] void should_not_cache_a_new_signer() => _cache.Count.ShouldEqual(0);
}
