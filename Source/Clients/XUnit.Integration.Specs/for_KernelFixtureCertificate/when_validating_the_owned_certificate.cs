// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFixtureCertificate;

public class when_validating_the_owned_certificate : Specification
{
    KernelFixtureCertificate _material;
    KernelFixtureCertificate _other;
    X509Certificate2 _certificate;
    X509Certificate2 _otherCertificate;
    RemoteCertificateValidationCallback _validate;

    void Establish()
    {
        _material = KernelFixtureCertificate.Create();
        _other = KernelFixtureCertificate.Create();
        _certificate = X509CertificateLoader.LoadCertificate(_material.PublicCertificate.AsSpan());
        _otherCertificate = X509CertificateLoader.LoadCertificate(_other.PublicCertificate.AsSpan());
    }

    void Because() => _validate = CertificateLoader.CreateServerCertificateValidationCallback(skipTlsValidation: false, pinnedCertificateHash: _material.Hash);

    async Task Destroy()
    {
        _certificate?.Dispose();
        _otherCertificate?.Dispose();
        if (_material is not null) await _material.DisposeAfter(() => ValueTask.CompletedTask);
        if (_other is not null) await _other.DisposeAfter(() => ValueTask.CompletedTask);
    }

    [Fact] void should_accept_the_owned_self_signed_certificate_with_only_chain_errors() => _validate(this, _certificate, null, SslPolicyErrors.RemoteCertificateChainErrors).ShouldBeTrue();
    [Fact] void should_refuse_another_untrusted_certificate() => _validate(this, _otherCertificate, null, SslPolicyErrors.RemoteCertificateChainErrors).ShouldBeFalse();
    [Fact] void should_refuse_the_owned_certificate_for_the_wrong_hostname() => _validate(this, _certificate, null, SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch).ShouldBeFalse();
    [Fact] void should_refuse_a_missing_certificate() => _validate(this, null, null, SslPolicyErrors.RemoteCertificateNotAvailable).ShouldBeFalse();
    [Fact] void should_refuse_the_owned_certificate_with_a_different_pin() => CertificateLoader.CreateServerCertificateValidationCallback(skipTlsValidation: false, pinnedCertificateHash: _other.Hash)(this, _certificate, null, SslPolicyErrors.RemoteCertificateChainErrors).ShouldBeFalse();
    [Fact] void should_preserve_existing_valid_chain_acceptance_without_claiming_strict_pinning() => _validate(this, _otherCertificate, null, SslPolicyErrors.None).ShouldBeTrue();
}
