// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.for_HttpFederationCaller;

public class when_reading_the_client_certificate : Specification
{
    const int FederationPort = 35100;
    const int MainPort = 35000;

    X509Certificate2 _certificate;
    IOptions<ChronicleOptions> _options;
    X509Certificate2? _onFederationPort;
    X509Certificate2? _onMainPort;
    X509Certificate2? _withoutCertificate;
    X509Certificate2? _withoutRequest;
    X509Certificate2? _whenDisabled;

    void Establish()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=peer", key, HashAlgorithmName.SHA256);
        _certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        _options = Options.Create(new ChronicleOptions { Federation = new() { Enabled = true, Port = FederationPort } });
    }

    void Because()
    {
        _onFederationPort = new HttpFederationCaller(AccessorFor(FederationPort, _certificate), _options).ClientCertificate;
        _onMainPort = new HttpFederationCaller(AccessorFor(MainPort, _certificate), _options).ClientCertificate;
        _withoutCertificate = new HttpFederationCaller(AccessorFor(FederationPort, null), _options).ClientCertificate;
        _withoutRequest = new HttpFederationCaller(new HttpContextAccessor(), _options).ClientCertificate;
        _whenDisabled = new HttpFederationCaller(
            AccessorFor(FederationPort, _certificate),
            Options.Create(new ChronicleOptions { Federation = new() { Enabled = false, Port = FederationPort } })).ClientCertificate;
    }

    void Destroy() => _certificate.Dispose();

    [Fact] void should_give_the_certificate_presented_on_the_federation_port() => _onFederationPort.ShouldEqual(_certificate);
    [Fact] void should_not_give_a_certificate_presented_on_another_port() => _onMainPort.ShouldBeNull();
    [Fact] void should_give_nothing_when_no_certificate_was_presented() => _withoutCertificate.ShouldBeNull();
    [Fact] void should_give_nothing_without_a_request() => _withoutRequest.ShouldBeNull();
    [Fact] void should_give_nothing_when_federation_is_disabled() => _whenDisabled.ShouldBeNull();

    static IHttpContextAccessor AccessorFor(int localPort, X509Certificate2? certificate)
    {
        var context = new DefaultHttpContext();
        context.Connection.LocalPort = localPort;
        context.Connection.ClientCertificate = certificate;
        return new HttpContextAccessor { HttpContext = context };
    }
}
