// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EncryptionCertificateRing.when_resolving_the_ring;

public class and_a_previous_certificate_is_configured : given.certificate_files
{
    EncryptionCertificateRing _ring;
    EncryptionCertificateRingEntry[] _all;

    void Because()
    {
        _ring = EncryptionCertificateRing.From(new Configuration.ChronicleOptions
        {
            EncryptionCertificate = Configuration(_activeCertificatePath, _previousCertificatePath)
        });
        _all = [.. _ring.All];
    }

    [Fact] void should_make_the_configured_certificate_active() => _ring.Active.KeyId.ShouldEqual(_activeCertificate.Thumbprint);
    [Fact] void should_keep_the_previous_certificate_for_decryption() => _ring.Previous.Single().KeyId.ShouldEqual(_previousCertificate.Thumbprint);
    [Fact] void should_offer_the_active_certificate_first() => _all[0].Role.ShouldEqual(EncryptionCertificateRole.Active);
    [Fact] void should_offer_the_previous_certificate_after_it() => _all[1].Role.ShouldEqual(EncryptionCertificateRole.Previous);
    [Fact] void should_hold_both_certificates() => _all.Length.ShouldEqual(2);
    [Fact] void should_find_the_previous_certificate_by_its_key_id() => _ring.Find(_previousCertificate.Thumbprint).Role.ShouldEqual(EncryptionCertificateRole.Previous);
    [Fact] void should_record_where_each_certificate_came_from() => _all.Select(_ => _.CertificatePath).ShouldContainOnly([_activeCertificatePath, _previousCertificatePath]);
}
