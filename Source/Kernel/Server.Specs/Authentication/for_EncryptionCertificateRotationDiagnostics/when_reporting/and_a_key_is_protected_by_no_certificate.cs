// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_EncryptionCertificateRotationDiagnostics.when_reporting;

public class and_a_key_is_protected_by_no_certificate : given.a_data_protection_key_ring
{
    EncryptionCertificateRotationReport _report;

    void Establish() => KeyRingHolds(KeyProtectedBy(_activeCertificate), KeyProtectedByNothing());

    void Because() => _report = DiagnosticsFor(_activeCertificate).GetReport();

    [Fact] void should_count_the_key_no_certificate_protects() => _report.KeysNotProtectedByCertificate.ShouldEqual(1);
    [Fact] void should_not_attribute_it_to_a_certificate() => _report.DataProtectionKeys.Single().KeyId.ShouldEqual(_activeCertificate.Thumbprint);
    [Fact] void should_not_report_it_as_a_retired_certificate() => _report.RetiredCertificatesInUse.ShouldBeFalse();
}
