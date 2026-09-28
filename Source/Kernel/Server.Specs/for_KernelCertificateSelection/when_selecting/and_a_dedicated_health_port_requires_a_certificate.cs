// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelCertificateSelection.when_selecting;

public class and_a_dedicated_health_port_requires_a_certificate : Specification
{
    X509Certificate2? _selectedCertificate;
    Exception _exception;

    void Because()
    {
        var options = new Configuration.ChronicleOptions
        {
            Tls = new() { Enabled = false },
            Authentication = new() { Enabled = false },
            Health = new() { Port = 8080 }
        };
        var logger = Substitute.For<ILogger<Kernel>>();
        _exception = Catch.Exception(() =>
        {
            _selectedCertificate = KernelCertificateSelection.Select(options, logger, developmentBuild: true);
            KernelListeners.Configure(options, _selectedCertificate, logger, (_, _, _) => { });
        });
    }

    [Fact] void should_not_generate_a_development_certificate_for_health() => _selectedCertificate.ShouldBeNull();
    [Fact] void should_fail_with_the_health_certificate_requirement() => _exception.Message.ShouldContain("Health:Tls");
}
