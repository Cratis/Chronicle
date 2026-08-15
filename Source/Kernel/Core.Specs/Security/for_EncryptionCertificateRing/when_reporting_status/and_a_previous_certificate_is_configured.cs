// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_EncryptionCertificateRing.when_reporting_status;

public class and_a_previous_certificate_is_configured : given.certificate_files
{
    EncryptionCertificateRingStatus _status;

    void Because() => _status = EncryptionCertificateRing.From(new Configuration.ChronicleOptions
    {
        EncryptionCertificate = Configuration(_activeCertificatePath, _previousCertificatePath)
    }).GetStatus();

    [Fact] void should_report_the_ring_as_configured() => _status.IsConfigured.ShouldBeTrue();
    [Fact] void should_name_the_active_key_id() => _status.ActiveKeyId.ShouldEqual(_activeCertificate.Thumbprint);
    [Fact] void should_report_both_certificates() => _status.Certificates.Count().ShouldEqual(2);
    [Fact] void should_report_the_active_certificate_first() => _status.Certificates.First().Role.ShouldEqual(EncryptionCertificateRole.Active);
    [Fact] void should_report_the_previous_certificate_as_previous() => _status.Certificates.Last().Role.ShouldEqual(EncryptionCertificateRole.Previous);
    [Fact] void should_report_the_ring_as_rotating() => _status.IsRotating.ShouldBeTrue();
    [Fact] void should_report_the_subject_of_each_certificate() => _status.Certificates.Select(_ => _.Subject).ShouldContainOnly([_activeCertificate.Subject, _previousCertificate.Subject]);
    [Fact] void should_report_where_each_certificate_was_loaded_from() => _status.Certificates.Select(_ => _.CertificatePath).ShouldContainOnly([_activeCertificatePath, _previousCertificatePath]);
    [Fact] void should_not_report_a_valid_certificate_as_expired() => _status.Certificates.Any(_ => _.HasExpired).ShouldBeFalse();
}
