// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelStartupDiagnostics.when_reporting_the_identity_provider_certificate;

public class and_certificate_is_configured : Specification
{
    ILogger<Kernel> _logger;

    void Establish()
    {
        _logger = Substitute.For<ILogger<Kernel>>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
    }

    void Because() => KernelStartupDiagnostics.ReportIdentityProviderCertificate(
        new() { IdentityProvider = new() { Certificate = new() { CertificatePath = "identity.pfx" } } }, _logger);

    [Fact] void should_warn_that_the_setting_is_ignored() => _logger.ReceivedCalls()
        .Any(_ => _.GetMethodInfo().Name == "Log" && _.GetArguments()[0] is LogLevel.Warning &&
                  _.GetArguments()[1] is EventId eventId && eventId.Name == "IdentityProviderCertificateIgnored")
        .ShouldBeTrue();
}
