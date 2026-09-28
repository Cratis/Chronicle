// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelCertificateSelection.when_selecting;

public class and_tls_is_enabled_without_a_production_certificate : Specification
{
    Exception _exception;

    void Because()
    {
        var options = new Configuration.ChronicleOptions();
        var logger = Substitute.For<ILogger<Kernel>>();
        _exception = Catch.Exception(() =>
        {
            var certificate = KernelCertificateSelection.Select(options, logger, developmentBuild: false);
            KernelListeners.Configure(options, certificate, logger, (_, _, _) => { });
        });
    }

    [Fact] void should_fail_before_opening_a_listener() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_missing_tls_certificate() => _exception.Message.ShouldContain("No TLS certificate is configured");
}
