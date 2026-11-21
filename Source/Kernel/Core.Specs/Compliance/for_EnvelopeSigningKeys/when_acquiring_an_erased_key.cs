// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Compliance.for_EnvelopeSigningKeys;

public class when_acquiring_an_erased_key : given.a_signing_key_cache
{
    EnvelopeSigningKey _signer;
    Exception _failure;
    Exception _revisionFailure;
    bool _signed;

    void Establish()
    {
        _signer = _cache.Acquire(_key, 7, _commitment);
        _cache.Evict(_commitment);
    }

    void Because()
    {
        _failure = Catch.Exception(() => _cache.Acquire(_key, 7, _commitment));
        _revisionFailure = Catch.Exception(() => _cache.Acquire(_key, 8, _commitment));
        _signed = _signer.TrySign("value"u8.ToArray(), out _);
    }

    [Fact] void should_refuse_to_create_another_signer() => _failure.ShouldBeOfExactType<CryptographicException>();
    [Fact] void should_not_allow_the_erased_material_at_another_revision() => _revisionFailure.ShouldBeOfExactType<CryptographicException>();
    [Fact] void should_not_cache_another_signer() => _cache.Count.ShouldEqual(0);
    [Fact] void should_not_sign_with_the_disposed_key() => _signed.ShouldBeFalse();
}
