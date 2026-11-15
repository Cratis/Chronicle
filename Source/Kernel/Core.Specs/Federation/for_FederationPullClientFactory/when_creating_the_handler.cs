// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Authentication;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Federation.for_FederationPullClientFactory;

public class when_creating_the_handler : given.certificates
{
    FederationPullClientFactory _factory;
    SocketsHttpHandler _handler;
    Exception _plainHttp;
    Exception _unknownCertificate;

    void Establish()
    {
        var certificates = Substitute.For<IFederationClientCertificates>();
        certificates.Get("client").Returns(_certificate);
        certificates.Get("missing").Throws(new InvalidOperationException("No client certificate named 'missing' is configured"));
        _factory = new(certificates);
    }

    void Because()
    {
        _handler = _factory.CreateHandler(_certificate, CertificatePin.From(_certificate).ToString());
        _plainHttp = Catch.Exception(() => _factory.Create(Binding("http://producer.example:35100", "client")));
        _unknownCertificate = Catch.Exception(() => _factory.Create(Binding("https://producer.example:35100", "missing")));
    }

    void Destroy() => _handler.Dispose();

    static FederationBindingState Binding(string endpoint, string certificate) =>
        new("b", "source", endpoint, "sha256:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=", certificate, "remote", "Default", "local", "Default", FederationOrigin.Runtime);

#pragma warning disable CA5398 // The specification asserts that TLS 1.3 is the only protocol, by design (decision 0027).
    [Fact] void should_allow_tls_1_3_only() => _handler.SslOptions.EnabledSslProtocols.ShouldEqual(SslProtocols.Tls13);
#pragma warning restore CA5398
    [Fact] void should_present_the_client_certificate() => _handler.SslOptions.ClientCertificates!.Count.ShouldEqual(1);
    [Fact] void should_validate_the_server_by_pin_and_not_by_chain() => _handler.SslOptions.RemoteCertificateValidationCallback.ShouldNotBeNull();
    [Fact] void should_accept_the_pinned_server() => _handler.SslOptions.RemoteCertificateValidationCallback!(this, _certificate, null, SslPolicyErrors.RemoteCertificateChainErrors).ShouldBeTrue();
    [Fact] void should_reject_any_other_server() => _handler.SslOptions.RemoteCertificateValidationCallback!(this, _otherCertificate, null, SslPolicyErrors.None).ShouldBeFalse();
    [Fact] void should_refuse_a_plain_http_endpoint() => _plainHttp.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_fail_when_the_client_certificate_is_unknown() => _unknownCertificate.ShouldBeOfExactType<InvalidOperationException>();
}
