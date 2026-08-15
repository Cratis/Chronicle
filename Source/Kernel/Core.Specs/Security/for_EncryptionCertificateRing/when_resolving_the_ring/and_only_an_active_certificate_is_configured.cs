// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EncryptionCertificateRing.when_resolving_the_ring;

public class and_only_an_active_certificate_is_configured : given.certificate_files
{
    EncryptionCertificateRing _ring;
    Exception _exception;

    void Because() => _exception = Catch.Exception(() =>
    {
        _ring = EncryptionCertificateRing.From(new Configuration.ChronicleOptions
        {
            EncryptionCertificate = Configuration(_activeCertificatePath)
        });
        _ = _ring.All.ToArray();
    });

    [Fact] void should_resolve_the_ring() => _exception.ShouldBeNull();
    [Fact] void should_consider_the_ring_configured() => _ring.IsConfigured.ShouldBeTrue();
    [Fact] void should_make_the_configured_certificate_active() => _ring.Active.KeyId.ShouldEqual(_activeCertificate.Thumbprint);
    [Fact] void should_hold_no_previous_certificates() => _ring.Previous.ShouldBeEmpty();
    [Fact] void should_hold_only_the_active_certificate() => _ring.All.Count().ShouldEqual(1);
    [Fact] void should_find_the_active_certificate_by_its_key_id() => _ring.Find(_activeCertificate.Thumbprint).ShouldNotBeNull();
}
