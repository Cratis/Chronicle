// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_EncryptionCertificate.when_binding_configuration;

public class and_previous_certificates_are_configured : Specification
{
    ChronicleOptions _options;
    PreviousEncryptionCertificate[] _previous;

    void Establish()
    {
        _options = new ChronicleOptions();
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Chronicle:EncryptionCertificate:CertificatePath"] = "/certs/encryption-2026.pfx",
                ["Cratis:Chronicle:EncryptionCertificate:CertificatePassword"] = "the-active-password",
                ["Cratis:Chronicle:EncryptionCertificate:Previous:0:CertificatePath"] = "/certs/encryption-2025.pfx",
                ["Cratis:Chronicle:EncryptionCertificate:Previous:0:CertificatePassword"] = "the-previous-password",
                ["Cratis:Chronicle:EncryptionCertificate:Previous:1:CertificatePath"] = "/certs/encryption-2024.pfx"
            })
            .Build()
            .GetSection(ChronicleOptions.SectionPath)
            .Bind(_options);

        _previous = [.. _options.EncryptionCertificate.Previous];
    }

    [Fact] void should_bind_the_active_certificate_path() => _options.EncryptionCertificate.CertificatePath.ShouldEqual("/certs/encryption-2026.pfx");
    [Fact] void should_bind_the_active_certificate_password() => _options.EncryptionCertificate.CertificatePassword.ShouldEqual("the-active-password");
    [Fact] void should_consider_the_certificate_configured() => _options.EncryptionCertificate.IsConfigured.ShouldBeTrue();
    [Fact] void should_bind_every_previous_certificate() => _previous.Length.ShouldEqual(2);
    [Fact] void should_keep_the_configured_order() => _previous[0].CertificatePath.ShouldEqual("/certs/encryption-2025.pfx");
    [Fact] void should_bind_a_previous_certificate_password() => _previous[0].CertificatePassword.ShouldEqual("the-previous-password");
    [Fact] void should_bind_a_previous_certificate_without_a_password() => _previous[1].CertificatePassword.ShouldBeNull();
}
