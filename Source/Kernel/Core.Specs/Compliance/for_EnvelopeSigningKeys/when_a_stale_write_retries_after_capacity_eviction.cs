// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_EnvelopeSigningKeys;

public class when_a_stale_write_retries_after_capacity_eviction : given.a_signing_key_cache
{
    EnvelopeSigningKeys.SigningOperation _operation;
    EnvelopeSigningKey _signer;
    Exception _failure;
    bool _signed;

    void Establish()
    {
        _cache = new(capacity: 1);
        _operation = _cache.Begin(_key, 7, _commitment);
        _signer = _operation.Acquire();
        using var other = _cache.Begin(_key, 7, [1]);
        other.Acquire();
        _cache.Evict(_commitment, new EncryptionKeyErasure(7, [_key.Fingerprint], false));
    }

    void Because()
    {
        _signed = _signer.TrySign("value"u8.ToArray(), out _);
        _failure = Catch.Exception(() => _operation.Acquire());
        _operation.Dispose();
    }

    [Fact] void should_not_sign_with_the_evicted_key() => _signed.ShouldBeFalse();
    [Fact] void should_refuse_the_retry_even_after_the_cache_entry_was_removed() => _failure.ShouldBeOfExactType<CryptographicException>();
    [Fact] void should_release_the_revoked_operation() => _cache.ActiveCount.ShouldEqual(0);
}
