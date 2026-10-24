// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.for_AddWebhookValidator.when_validating;

public class and_dns_is_temporarily_unavailable : for_WebhookRegistrar.given.a_webhook_registrar
{
    [Theory]
    [InlineData(WebhookValidationMode.Enforce)]
    [InlineData(WebhookValidationMode.Warn)]
    async Task should_return_a_clear_validation_failure_without_probing(WebhookValidationMode mode)
    {
        var resolver = Substitute.For<IWebhookHostResolver>();
        resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IPAddress[]>(new SocketException((int)SocketError.TryAgain)));
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        var options = Options.Create(new ChronicleOptions { Webhooks = new() { Validation = mode, AllowedHosts = ["receiver.example"] } });
        var policy = new WebhookTargetPolicy(options, environment, resolver, NullLogger<WebhookTargetPolicy>.Instance);
        var validator = new AddWebhookValidator(_registrar, _storage, policy);

        var result = await validator.ValidateAsync(new AddWebhook("orders", "receiver", "https://receiver.example/hook", "event-log", [], AuthorizationType.None, "", "", "", "", "", "", new Dictionary<string, string>(), true, true));

        result.IsValid.ShouldBeFalse();
        result.Errors.Single().ErrorMessage.ShouldEqual("Webhook target host resolution is temporarily unavailable. Please retry.");
        await _webhookMediator.DidNotReceiveWithAnyArgs().OnNext(default, default!, default!, default!, default, default);
    }
}
