// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_EnvelopeSigningKeys;

public class when_starting_a_new_key_lifecycle : given.a_signing_key_cache
{
    bool _signed;

    void Establish()
    {
        using var operation = _cache.Begin(_key, 7, _commitment);
        operation.Acquire();
        _cache.Evict(_commitment, new EncryptionKeyErasure(7, [_key.Fingerprint], false));
        _key = new Encryption(null, new()).GenerateKey();
    }

    void Because()
    {
        using var operation = _cache.Begin(_key, 8, _commitment);
        _signed = operation.Acquire().TrySign("value"u8.ToArray(), out _);
    }

    [Fact] void should_allow_the_new_material() => _signed.ShouldBeTrue();
    [Fact] void should_cache_the_new_signer() => _cache.Count.ShouldEqual(1);
}
