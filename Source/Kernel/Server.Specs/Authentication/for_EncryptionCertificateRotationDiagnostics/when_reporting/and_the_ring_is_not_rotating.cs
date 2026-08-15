// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_EncryptionCertificateRotationDiagnostics.when_reporting;

public class and_the_ring_is_not_rotating : given.a_data_protection_key_ring
{
    EncryptionCertificateRotationReport _report;

    void Establish() => KeyRingHolds(KeyProtectedBy(_activeCertificate));

    void Because() => _report = DiagnosticsFor(_activeCertificate).GetReport();

    [Fact] void should_not_describe_the_ring_as_rotating() => _report.Ring.IsRotating.ShouldBeFalse();
    [Fact] void should_have_nothing_to_retire() => _report.CanRetirePreviousCertificates.ShouldBeFalse();
    [Fact] void should_report_no_dependency_on_a_previous_certificate() => _report.PreviousCertificatesInUse.ShouldBeFalse();
}
