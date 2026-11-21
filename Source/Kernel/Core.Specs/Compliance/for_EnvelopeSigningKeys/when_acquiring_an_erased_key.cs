// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_EnvelopeSigningKeys;

public class when_acquiring_an_erased_key : given.a_signing_key_cache
{
    EnvelopeSigningKey _signer;
    EnvelopeSigningKeys.SigningOperation _operation;
    EnvelopeSigningKeys.SigningOperation _otherRevision;
    Exception _failure;
    Exception _revisionFailure;
    bool _signed;

    void Establish()
    {
        _operation = _cache.Begin(_key, 7, _commitment);
        _otherRevision = _cache.Begin(_key, 8, _commitment);
        _signer = _operation.Acquire();
        _cache.Evict(_commitment, new EncryptionKeyErasure(7, [_key.Fingerprint], false));
    }

    void Because()
    {
        _failure = Catch.Exception(() => _operation.Acquire());
        _revisionFailure = Catch.Exception(() => _otherRevision.Acquire());
        _signed = _signer.TrySign("value"u8.ToArray(), out _);
        _operation.Dispose();
        _otherRevision.Dispose();
    }

    [Fact] void should_refuse_to_create_another_signer() => _failure.ShouldBeOfExactType<CryptographicException>();
    [Fact] void should_not_allow_the_erased_material_at_another_revision() => _revisionFailure.ShouldBeOfExactType<CryptographicException>();
    [Fact] void should_not_cache_another_signer() => _cache.Count.ShouldEqual(0);
    [Fact] void should_not_sign_with_the_disposed_key() => _signed.ShouldBeFalse();
    [Fact] void should_release_the_revocation_bookkeeping() => _cache.ActiveCount.ShouldEqual(0);
}
