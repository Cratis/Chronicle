// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelStartupDiagnostics.when_reporting_ignored_development_only_settings;

public class and_a_default_admin_password_is_configured_in_a_production_build : Specification
{
    const string Value = "not-a-real-secret";
    ILogger<Kernel> _logger;

    void Establish()
    {
        _logger = Substitute.For<ILogger<Kernel>>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
    }

    void Because() => KernelStartupDiagnostics.ReportIgnoredDevelopmentOnlySettings(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Cratis:Chronicle:Authentication:DefaultAdminPassword"] = Value }).Build(),
        _logger,
        false);

    [Fact] void should_warn_that_it_is_ignored() => _logger.ReceivedCalls()
        .Any(_ => _.GetMethodInfo().Name == "Log" && _.GetArguments()[0] is LogLevel.Warning &&
                  _.GetArguments()[1] is EventId eventId && eventId.Name == "DefaultAdminPasswordIgnored")
        .ShouldBeTrue();

    [Fact] void should_not_log_the_value() => _logger.ReceivedCalls()
        .Any(_ => _.GetArguments().Any(argument => argument?.ToString()?.Contains(Value) == true))
        .ShouldBeFalse();
}
