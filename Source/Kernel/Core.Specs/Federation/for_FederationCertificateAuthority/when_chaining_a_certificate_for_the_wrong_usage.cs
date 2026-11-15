// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.for_FederationCertificateAuthority;

public class when_chaining_a_certificate_for_the_wrong_usage : Specification
{
    X509Certificate2 _authority;
    X509Certificate2 _server;
    X509Certificate2 _client;
    DateTimeOffset _now;
    bool _serverAsClient;
    bool _clientAsServer;
    bool _serverAsServer;
    bool _clientAsClient;

    void Establish()
    {
        _now = DateTimeOffset.UtcNow;
        _authority = FederationCertificateAuthority.CreateAuthority("example", _now.AddDays(-1), _now.AddYears(1));
        using var serverKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        _server = FederationCertificateAuthority.IssueServer(_authority, Csr(serverKey), "chronicle.example.com", _now.AddMinutes(-1), _now.AddDays(1));
        _client = FederationCertificateAuthority.IssueClient(_authority, Csr(clientKey), "spiffe://example/customer/acme/kernel/prod", _now.AddMinutes(-1), _now.AddDays(1));
    }

    void Because()
    {
        _serverAsClient = FederationCertificateAuthority.ChainsTo(_server, _authority, _now, FederationCertificateAuthority.ClientAuthentication);
        _clientAsServer = FederationCertificateAuthority.ChainsTo(_client, _authority, _now, FederationCertificateAuthority.ServerAuthentication);
        _serverAsServer = FederationCertificateAuthority.ChainsTo(_server, _authority, _now, FederationCertificateAuthority.ServerAuthentication);
        _clientAsClient = FederationCertificateAuthority.ChainsTo(_client, _authority, _now, FederationCertificateAuthority.ClientAuthentication);
    }

    void Destroy()
    {
        _authority.Dispose();
        _server.Dispose();
        _client.Dispose();
    }

    [Fact] void should_not_accept_a_server_certificate_as_a_client() => _serverAsClient.ShouldBeFalse();
    [Fact] void should_not_accept_a_client_certificate_as_a_server() => _clientAsServer.ShouldBeFalse();
    [Fact] void should_accept_a_server_certificate_as_a_server() => _serverAsServer.ShouldBeTrue();
    [Fact] void should_accept_a_client_certificate_as_a_client() => _clientAsClient.ShouldBeTrue();

    static byte[] Csr(ECDsa key) => new CertificateRequest("CN=requester", key, HashAlgorithmName.SHA256).CreateSigningRequest();
}
