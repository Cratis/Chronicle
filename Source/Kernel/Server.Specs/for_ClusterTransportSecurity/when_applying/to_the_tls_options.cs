// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Connections.Security;

namespace Cratis.Chronicle.Server.for_ClusterTransportSecurity.when_applying;

public class to_the_tls_options : given.a_certificate
{
    TlsOptions _options;

    void Because()
    {
        _options = new TlsOptions();
        ClusterTransportSecurity.Apply(_options, new Cratis.Chronicle.Configuration.Tls(), _certificate);
    }

    [Fact] void should_present_the_certificate_as_server() => _options.LocalServerCertificateSelector(null!, null).ShouldEqual(_x509);
    [Fact] void should_require_a_certificate_from_the_peer() => _options.RemoteCertificateMode.ShouldEqual(RemoteCertificateMode.RequireCertificate);
    [Fact] void should_require_a_certificate_from_connecting_silos() => _options.ClientCertificateMode.ShouldEqual(RemoteCertificateMode.RequireCertificate);
    [Fact] void should_validate_the_peer_certificate() => _options.RemoteCertificateValidation.ShouldNotBeNull();
}
