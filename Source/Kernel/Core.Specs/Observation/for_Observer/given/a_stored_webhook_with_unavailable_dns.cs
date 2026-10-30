// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_stored_webhook_with_unavailable_dns : an_observer
{
    protected bool _dnsAvailable;
    protected SocketError _dnsError = SocketError.TryAgain;

    protected virtual WebhookValidationMode ValidationMode => WebhookValidationMode.Enforce;

    void Establish()
    {
        var resolver = Substitute.For<IWebhookHostResolver>();
        resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => _dnsAvailable
            ? Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") })
            : Task.FromException<IPAddress[]>(new SocketException((int)_dnsError)));
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        var options = Options.Create(new ChronicleOptions
        {
            Webhooks = new() { Validation = ValidationMode, AllowedHosts = ["receiver.example"] }
        });
        var policy = new WebhookTargetPolicy(options, environment, resolver, NullLogger<WebhookTargetPolicy>.Instance);
        _webhookTargetPolicy.EvaluateForDelivery(Arg.Any<string>(), Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>())
            .Returns(call => policy.EvaluateForDelivery(call.Arg<string>(), call.Arg<EventStoreName?>(), call.Arg<CancellationToken>()));
    }
}
