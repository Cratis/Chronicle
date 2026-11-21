// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.for_EnvelopeSigningKeys;

public class when_starting_a_new_key_lifecycle : given.a_signing_key_cache
{
    bool _signed;

    void Establish()
    {
        _cache.Acquire(_key, 7, _commitment);
        _cache.Evict(_commitment);
        _key = new Encryption(null, new()).GenerateKey();
    }

    void Because() => _signed = _cache.Acquire(_key, 8, _commitment).TrySign("value"u8.ToArray(), out _);

    [Fact] void should_allow_the_new_material() => _signed.ShouldBeTrue();
    [Fact] void should_cache_the_new_signer() => _cache.Count.ShouldEqual(1);
}
