// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Server.Authentication.for_EncryptionCertificateRotationDiagnostics.when_reporting;

public class and_keys_still_depend_on_a_previous_certificate : given.a_data_protection_key_ring
{
    EncryptionCertificateRotationReport _report;
    DataProtectionKeyDependency _previousDependency;

    void Establish() => KeyRingHolds(
        KeyProtectedBy(_activeCertificate),
        KeyProtectedBy(_previousCertificate),
        KeyProtectedBy(_previousCertificate));

    void Because()
    {
        _report = DiagnosticsFor(_activeCertificate, _previousCertificate).GetReport();
        _previousDependency = _report.DataProtectionKeys.Single(_ => _.KeyId == _previousCertificate.Thumbprint);
    }

    [Fact] void should_report_the_dependency_on_the_previous_certificate() => _report.PreviousCertificatesInUse.ShouldBeTrue();
    [Fact] void should_report_the_previous_role() => _previousDependency.Role.ShouldEqual(EncryptionCertificateRole.Previous);
    [Fact] void should_count_the_keys_that_depend_on_it() => _previousDependency.KeyCount.ShouldEqual(2);
    [Fact] void should_refuse_to_say_the_previous_certificate_can_be_retired() => _report.CanRetirePreviousCertificates.ShouldBeFalse();
    [Fact] void should_report_both_certificates() => _report.DataProtectionKeys.Count().ShouldEqual(2);
    [Fact] void should_not_report_a_retired_certificate() => _report.RetiredCertificatesInUse.ShouldBeFalse();
}
