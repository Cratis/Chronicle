// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelCertificateSelection.when_selecting;

public class and_tls_is_disabled_without_a_certificate : Specification
{
    X509Certificate2? _certificate;
    HttpProtocols _protocols;

    void Because()
    {
        var options = new Configuration.ChronicleOptions
        {
            Tls = new() { Enabled = false },
            Authentication = new() { Enabled = false }
        };
        var logger = Substitute.For<ILogger<Kernel>>();
        _certificate = KernelCertificateSelection.Select(options, logger, developmentBuild: true);
        KernelListeners.Configure(options, _certificate, logger, (_, protocols, _) => _protocols = protocols);
    }

    [Fact] void should_not_generate_a_development_certificate() => _certificate.ShouldBeNull();
    [Fact] void should_serve_cleartext_http2() => _protocols.ShouldEqual(HttpProtocols.Http2);
}
