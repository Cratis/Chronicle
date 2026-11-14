// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.for_FederationServerChainValidator;

public class when_validating_a_server : Specification
{
    X509Certificate2 _authority;
    X509Certificate2 _otherAuthority;
    X509Certificate2 _server;
    X509Certificate2 _serverFromOtherAuthority;
    DateTimeOffset _now;
    bool _trusted;
    bool _withNameMismatch;
    bool _fromAnotherAuthority;
    bool _whenExpired;
    bool _withoutCertificate;
    bool _withChainErrorsOnly;

    void Establish()
    {
        _now = DateTimeOffset.UtcNow;
        _authority = FederationCertificateAuthority.CreateAuthority("example", _now.AddDays(-1), _now.AddYears(1));
        _otherAuthority = FederationCertificateAuthority.CreateAuthority("example", _now.AddDays(-1), _now.AddYears(1));
        _server = Issue(_authority);
        _serverFromOtherAuthority = Issue(_otherAuthority);
    }

    void Destroy()
    {
        _authority.Dispose();
        _otherAuthority.Dispose();
        _server.Dispose();
        _serverFromOtherAuthority.Dispose();
    }

    void Because()
    {
        _trusted = FederationServerChainValidator.Validate(_server, SslPolicyErrors.None, _authority, _now);
        _withChainErrorsOnly = FederationServerChainValidator.Validate(_server, SslPolicyErrors.RemoteCertificateChainErrors, _authority, _now);
        _withNameMismatch = FederationServerChainValidator.Validate(_server, SslPolicyErrors.RemoteCertificateNameMismatch, _authority, _now);
        _fromAnotherAuthority = FederationServerChainValidator.Validate(_serverFromOtherAuthority, SslPolicyErrors.RemoteCertificateChainErrors, _authority, _now);
        _whenExpired = FederationServerChainValidator.Validate(_server, SslPolicyErrors.RemoteCertificateChainErrors, _authority, _now.AddDays(60));
        _withoutCertificate = FederationServerChainValidator.Validate(null, SslPolicyErrors.RemoteCertificateNotAvailable, _authority, _now);
    }

    [Fact] void should_trust_a_server_that_chains_to_the_authority() => _trusted.ShouldBeTrue();
    [Fact] void should_trust_it_although_the_system_does_not_know_the_authority() => _withChainErrorsOnly.ShouldBeTrue();
    [Fact] void should_not_trust_a_server_for_another_host() => _withNameMismatch.ShouldBeFalse();
    [Fact] void should_not_trust_a_server_signed_by_another_authority() => _fromAnotherAuthority.ShouldBeFalse();
    [Fact] void should_not_trust_an_expired_server_certificate() => _whenExpired.ShouldBeFalse();
    [Fact] void should_not_trust_a_server_without_a_certificate() => _withoutCertificate.ShouldBeFalse();

    X509Certificate2 Issue(X509Certificate2 authority)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=federation.example.com", key, HashAlgorithmName.SHA256).CreateSigningRequest();
        return FederationCertificateAuthority.IssueServer(authority, request, "federation.example.com", _now.AddMinutes(-5), _now.AddDays(30));
    }
}
