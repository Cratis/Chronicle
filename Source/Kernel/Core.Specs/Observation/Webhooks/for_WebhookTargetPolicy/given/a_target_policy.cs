// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookTargetPolicy.given;

public class a_target_policy : Specification
{
    protected IWebhookHostResolver _resolver;
    protected ILogger<WebhookTargetPolicy> _logger;
    protected WebhookTargetPolicy _policy;
    protected WebhookTargetValidation _result;

    void Establish()
    {
        _resolver = Substitute.For<IWebhookHostResolver>();
        _resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([IPAddress.Parse("93.184.216.34")]);
        _logger = Substitute.For<ILogger<WebhookTargetPolicy>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    protected void Configure(Configuration.Webhooks options, bool development = false)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(development ? Environments.Development : Environments.Production);
        _policy = new WebhookTargetPolicy(Options.Create(new ChronicleOptions { Webhooks = options }), environment, _resolver, _logger);
    }
}
