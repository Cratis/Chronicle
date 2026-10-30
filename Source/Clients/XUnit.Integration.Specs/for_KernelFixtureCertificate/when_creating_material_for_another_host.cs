// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFixtureCertificate;

public class when_creating_material_for_another_host : Specification
{
    KernelFixtureCertificate _material;
    X509Certificate2 _certificate;

    void Because()
    {
        _material = KernelFixtureCertificate.CreateForHost("not-localhost.invalid");
        _certificate = X509CertificateLoader.LoadCertificate(_material.PublicCertificate.AsSpan());
    }

    async Task Destroy()
    {
        _certificate.Dispose();
        await _material.DisposeAfter(() => ValueTask.CompletedTask);
    }

    [Fact] void should_include_only_the_alternate_dns_name() =>
        _certificate.Extensions.OfType<X509SubjectAlternativeNameExtension>().Single().EnumerateDnsNames().Single().ShouldEqual("not-localhost.invalid");

    [Fact] void should_not_include_loopback_ip_addresses() =>
        _certificate.Extensions.OfType<X509SubjectAlternativeNameExtension>().Single().EnumerateIPAddresses().ShouldBeEmpty();

    [Fact] void should_refuse_a_name_mismatch_even_with_the_matching_pin() =>
        CertificateLoader.CreateServerCertificateValidationCallback(false, _material.Hash)(
            this, _certificate, null, SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors).ShouldBeFalse();
}
