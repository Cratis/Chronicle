// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.for_AddWebhookValidator.when_validating;

public class and_dns_is_temporarily_unavailable : for_WebhookRegistrar.given.a_webhook_registrar
{
    [Theory]
    [InlineData(WebhookValidationMode.Enforce)]
    [InlineData(WebhookValidationMode.Warn)]
    async Task should_apply_the_configured_validation_mode_without_throwing(WebhookValidationMode mode)
    {
        var resolver = Substitute.For<IWebhookHostResolver>();
        resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IPAddress[]>(new SocketException((int)SocketError.TryAgain)));
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        var options = Options.Create(new ChronicleOptions { Webhooks = new() { Validation = mode, AllowedHosts = ["receiver.example"] } });
        var logger = Substitute.For<ILogger<WebhookTargetPolicy>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var policy = new WebhookTargetPolicy(options, environment, resolver, logger);
        _webhookMediator.OnNext(default, default!, default!, default!, default, default).ReturnsForAnyArgs(Cratis.Monads.Catch.Success());
        var validator = new AddWebhookValidator(_registrar, _storage, policy);

        var result = await validator.ValidateAsync(new AddWebhook("orders", "receiver", "https://receiver.example/hook", "event-log", [], AuthorizationType.None, "", "", "", "", "", "", new Dictionary<string, string>(), true, true));

        result.IsValid.ShouldEqual(mode == WebhookValidationMode.Warn);
        if (mode == WebhookValidationMode.Warn)
        {
            logger.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Log" && call.GetArguments()[0] is LogLevel.Warning).ShouldBeTrue();
            await _webhookMediator.ReceivedWithAnyArgs().OnNext(default, default!, default!, default!, default, default);
        }
        else
        {
            result.Errors.Single().ErrorMessage.ShouldEqual("Webhook target host resolution is temporarily unavailable. Please retry.");
            await _webhookMediator.DidNotReceiveWithAnyArgs().OnNext(default, default!, default!, default!, default, default);
        }
    }
}
