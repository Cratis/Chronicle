// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Compliance.for_EnvelopeSigningKeys;

public class when_erasure_happens_during_lazy_creation : given.a_signing_key_cache
{
    EnvelopeSigningKey _created;
    Exception _failure;
    bool _signed;
    int _creations;

    void Establish()
    {
        _cache = new(certify: (key, _, commitment) =>
        {
            _creations++;
            _created = new(ECDsa.Create(ECCurve.NamedCurves.nistP256), [], [], 10, TimeSpan.FromMinutes(1), TimeProvider.System);
            _cache.Evict(commitment);
            return _created;
        });
    }

    void Because()
    {
        _failure = Catch.Exception(() => _cache.Acquire(_key, 7, _commitment));
        _signed = _created.TrySign("value"u8.ToArray(), out _);
    }

    [Fact] void should_refuse_the_in_flight_acquisition() => _failure.ShouldBeOfExactType<CryptographicException>();
    [Fact] void should_not_create_a_replacement() => _creations.ShouldEqual(1);
    [Fact] void should_not_keep_the_late_signer_cached() => _cache.Count.ShouldEqual(0);
    [Fact] void should_dispose_the_late_signer() => _created.IsUsable.ShouldBeFalse();
    [Fact] void should_not_sign_with_the_disposed_key() => _signed.ShouldBeFalse();
}
