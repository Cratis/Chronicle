// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelCertificateSelection.when_selecting;

public class and_only_the_health_listener_uses_a_configured_certificate : Cratis.Chronicle.Server.for_CertificateLoader.given.a_certificate_file
{
    X509Certificate2? _selected;
    readonly List<(HttpProtocols Protocols, X509Certificate2? Certificate)> _listeners = [];

    void Establish() => WritePkcs12(password: null);

    void Because()
    {
        var options = new Configuration.ChronicleOptions
        {
            Tls = new() { Enabled = false, CertificatePath = _certificatePath },
            Authentication = new() { Enabled = false },
            Health = new() { Port = 8080 }
        };
        var logger = Substitute.For<ILogger<Kernel>>();
        _selected = KernelCertificateSelection.Select(options, logger, developmentBuild: false);
        KernelListeners.Configure(options, _selected, logger, (_, protocols, cert) => _listeners.Add((protocols, cert)));
    }

    [Fact] void should_not_use_https_on_the_main_port() => _listeners[0].Certificate.ShouldBeNull();
    [Fact] void should_use_the_configured_certificate_on_the_health_port() => _listeners[1].Certificate!.Thumbprint.ShouldEqual(_sourceCertificate.Thumbprint);

    void Destroy() => _selected?.Dispose();
}
