// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Server.Authentication.for_EncryptionCertificateRotationDiagnostics.when_reporting;

public class and_every_key_is_protected_by_the_active_certificate : given.a_data_protection_key_ring
{
    EncryptionCertificateRotationReport _report;

    void Establish() => KeyRingHolds(KeyProtectedBy(_activeCertificate), KeyProtectedBy(_activeCertificate));

    void Because() => _report = DiagnosticsFor(_activeCertificate, _previousCertificate).GetReport();

    [Fact] void should_attribute_the_keys_to_the_active_certificate() => _report.DataProtectionKeys.Single().KeyId.ShouldEqual(_activeCertificate.Thumbprint);
    [Fact] void should_count_every_key() => _report.DataProtectionKeys.Single().KeyCount.ShouldEqual(2);
    [Fact] void should_report_the_active_role() => _report.DataProtectionKeys.Single().Role.ShouldEqual(EncryptionCertificateRole.Active);
    [Fact] void should_not_report_a_dependency_on_a_previous_certificate() => _report.PreviousCertificatesInUse.ShouldBeFalse();
    [Fact] void should_say_the_previous_certificate_can_be_retired() => _report.CanRetirePreviousCertificates.ShouldBeTrue();
    [Fact] void should_report_no_unprotected_keys() => _report.KeysNotProtectedByCertificate.ShouldEqual(0);
    [Fact] void should_report_the_ring_it_is_running_with() => _report.Ring.ActiveKeyId.ShouldEqual(_activeCertificate.Thumbprint);
}
