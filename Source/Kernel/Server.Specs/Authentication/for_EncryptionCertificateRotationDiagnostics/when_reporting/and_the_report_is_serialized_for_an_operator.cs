// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Server.Authentication.for_EncryptionCertificateRotationDiagnostics.when_reporting;

public class and_the_report_is_serialized_for_an_operator : given.a_data_protection_key_ring
{
    static readonly JsonSerializerOptions _asAnEndpointWould = new(JsonSerializerDefaults.Web);

    string _json;

    void Establish() => KeyRingHolds(KeyProtectedBy(_activeCertificate), KeyProtectedBy(_previousCertificate));

    void Because() => _json = JsonSerializer.Serialize(
        DiagnosticsFor(_activeCertificate, _previousCertificate).GetReport(),
        _asAnEndpointWould);

    [Fact] void should_name_the_ring() => _json.Contains("\"ring\":", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_active_key_id() => _json.Contains("\"activeKeyId\":", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_certificates() => _json.Contains("\"certificates\":", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_data_protection_keys() => _json.Contains("\"dataProtectionKeys\":", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_keys_no_certificate_protects() => _json.Contains("\"keysNotProtectedByCertificate\":", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_whether_a_previous_certificate_is_in_use() => _json.Contains("\"previousCertificatesInUse\":true", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_whether_a_retired_certificate_is_in_use() => _json.Contains("\"retiredCertificatesInUse\":false", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_whether_the_previous_certificates_can_be_retired() => _json.Contains("\"canRetirePreviousCertificates\":false", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_write_roles_by_name_rather_than_by_number() => _json.Contains("\"role\":\"Previous\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_never_write_key_material() => _json.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
}
