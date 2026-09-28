// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelListeners.given;

public class listeners : Specification
{
    protected readonly List<(int Port, HttpProtocols Protocols, X509Certificate2? Certificate)> _listeners = [];
    protected ILogger<Kernel> _logger;
    protected X509Certificate2 _certificate;

    void Establish()
    {
        _logger = Substitute.For<ILogger<Kernel>>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _certificate = DevelopmentCertificate.Create();
    }

    void Destroy() => _certificate.Dispose();

    protected void Configure(Configuration.ChronicleOptions options, X509Certificate2? certificate) =>
        KernelListeners.Configure(options, certificate, _logger, (port, protocols, cert) => _listeners.Add((port, protocols, cert)));
}
