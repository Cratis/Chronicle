// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookValidationStartup;

public class when_starting_outside_development : Specification
{
    WebhookValidationStartup _startup;
    ILogger<WebhookValidationStartup> _logger;

    void Establish()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        _logger = Substitute.For<ILogger<WebhookValidationStartup>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _startup = new WebhookValidationStartup(Options.Create(new ChronicleOptions { Webhooks = new() { AllowInsecureHttp = true } }), environment, _logger);
    }

    Task Because() => _startup.StartAsync(CancellationToken.None);

    [Fact] void should_warn_that_insecure_http_is_ignored() => _logger.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Log").ShouldBeTrue();
}
