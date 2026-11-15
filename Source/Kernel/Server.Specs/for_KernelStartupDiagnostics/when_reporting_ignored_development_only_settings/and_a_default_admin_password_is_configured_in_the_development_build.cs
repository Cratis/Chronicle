// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelStartupDiagnostics.when_reporting_ignored_development_only_settings;

public class and_a_default_admin_password_is_configured_in_the_development_build : Specification
{
    ILogger<Kernel> _logger;

    void Establish() => _logger = Substitute.For<ILogger<Kernel>>();

    void Because() => KernelStartupDiagnostics.ReportIgnoredDevelopmentOnlySettings(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Cratis:Chronicle:Authentication:DefaultAdminPassword"] = "not-a-real-secret" }).Build(),
        _logger,
        true);

    [Fact] void should_not_warn() => _logger.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Log").ShouldBeFalse();
}
