// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelStartupDiagnostics.when_reporting_the_identity_provider_certificate;

public class and_certificate_is_not_configured : Specification
{
    ILogger<Kernel> _logger;

    void Establish()
    {
        _logger = Substitute.For<ILogger<Kernel>>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
    }

    void Because() => KernelStartupDiagnostics.ReportIdentityProviderCertificate(new(), _logger);

    [Fact] void should_not_warn() => _logger.ReceivedCalls().ShouldBeEmpty();
}
