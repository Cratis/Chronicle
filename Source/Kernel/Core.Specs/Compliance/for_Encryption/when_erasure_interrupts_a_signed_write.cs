// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Releases;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_Encryption;

public class when_erasure_interrupts_a_signed_write : given.a_signed_value
{
    EnvelopeSigningKeys _cache;
    EnvelopeSigningKey _created;
    Exception _failure;
    int _creations;

    void Establish()
    {
        _cache = new(certify: (key, _, commitment) =>
        {
            _creations++;
            _created = new(ECDsa.Create(ECCurve.NamedCurves.nistP256), [], [], 10, TimeSpan.FromMinutes(1), TimeProvider.System);
            _cache.Evict(commitment, new EncryptionKeyErasure(7, [key.Fingerprint], false));
            return _created;
        });
        var releases = Substitute.For<IClusterReleases>();
        releases.Summarize().Returns(new ClusterReleaseSummary([(ClusterReleaseBoundary.KeyIdentity, 1)], 0, 0));
        _encryption = new(releases, _cache);
    }

    void Because() => _failure = Catch.Exception(() => _encryption.Encrypt("value"u8.ToArray(), _key, 7, _address));

    [Fact] void should_refuse_the_stale_write() => _failure.ShouldBeOfExactType<CryptographicException>();
    [Fact] void should_not_certify_a_replacement() => _creations.ShouldEqual(1);
    [Fact] void should_dispose_the_late_signer() => _created.IsUsable.ShouldBeFalse();
    [Fact] void should_leave_no_cached_signer() => _cache.Count.ShouldEqual(0);
    [Fact] void should_release_the_write_lease_when_encryption_fails() => _cache.ActiveCount.ShouldEqual(0);
}
