// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.ExternalServices.for_ExternalServiceTargetPolicy.given;

public class a_capture_target_policy : Specification
{
    protected ExternalServiceTargetPolicy _policy;
    protected WebhookTargetValidation _result;

    void Establish() => Configure(new());

    protected void Configure(Configuration.ExternalServices settings)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        var resolver = Substitute.For<IWebhookHostResolver>();
        resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([IPAddress.Parse("93.184.216.34")]);
        _policy = new(Options.Create(new ChronicleOptions { ExternalServices = settings }), environment, resolver, Substitute.For<ILogger<WebhookTargetPolicy>>());
    }
}
