// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EncryptionCertificateRing.when_resolving_the_ring;

public class and_nothing_is_configured : given.certificate_files
{
    EncryptionCertificateRing _ring;
    Exception _exception;

    void Because()
    {
        _ring = EncryptionCertificateRing.From(new Configuration.ChronicleOptions());
        _exception = Catch.Exception(() => _ = _ring.Active);
    }

    [Fact] void should_not_consider_the_ring_configured() => _ring.IsConfigured.ShouldBeFalse();
    [Fact] void should_hold_no_certificates() => _ring.All.ShouldBeEmpty();
    [Fact] void should_report_the_missing_certificate_when_one_is_asked_for() => _exception.ShouldBeOfExactType<EncryptionCertificateNotConfigured>();
    [Fact] void should_describe_the_ring_as_unconfigured() => _ring.GetStatus().IsConfigured.ShouldBeFalse();
}
