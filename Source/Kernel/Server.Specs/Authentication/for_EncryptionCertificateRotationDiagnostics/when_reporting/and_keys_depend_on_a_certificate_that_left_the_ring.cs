// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Server.Authentication.for_EncryptionCertificateRotationDiagnostics.when_reporting;

public class and_keys_depend_on_a_certificate_that_left_the_ring : given.a_data_protection_key_ring
{
    EncryptionCertificateRotationReport _report;
    DataProtectionKeyDependency _retiredDependency;

    void Establish() => KeyRingHolds(KeyProtectedBy(_activeCertificate), KeyProtectedBy(_retiredCertificate));

    void Because()
    {
        _report = DiagnosticsFor(_activeCertificate, _previousCertificate).GetReport();
        _retiredDependency = _report.DataProtectionKeys.Single(_ => _.KeyId == _retiredCertificate.Thumbprint);
    }

    [Fact] void should_report_that_data_depends_on_a_retired_certificate() => _report.RetiredCertificatesInUse.ShouldBeTrue();
    [Fact] void should_report_the_retired_role() => _retiredDependency.Role.ShouldEqual(EncryptionCertificateRole.Retired);
    [Fact] void should_name_the_certificate_that_is_missing() => _retiredDependency.KeyId.ShouldEqual(_retiredCertificate.Thumbprint);
    [Fact] void should_count_the_keys_that_are_already_unreadable() => _retiredDependency.KeyCount.ShouldEqual(1);
}
