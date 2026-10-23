// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_delivering;

public class and_the_receiver_exposes_the_managed_identity_token : Specification
{
    const string Token = "synthetic-secret-access-token";
    readonly List<WebhookDeliveryResult> _results = [];
    readonly CapturedLogs _logs = new();
    int _authenticatedRequests;

    async Task Because()
    {
        foreach (var status in new[] { HttpStatusCode.Unauthorized, HttpStatusCode.InternalServerError })
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(_logs));
            services.AddHttpClient(WebhookHttpClientFactory.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new Receiver(status, () => _authenticatedRequests++));
            using var provider = services.BuildServiceProvider();
            var tokens = Substitute.For<IManagedIdentityTokens>();
            tokens.AcquireToken(Arg.Any<ManagedIdentityAuthorization>(), Arg.Any<CancellationToken>()).Returns(new AccessTokenInfo(Token, DateTimeOffset.MaxValue));
            var policy = Substitute.For<IWebhookTargetPolicy>();
            policy.CheckManagedIdentity(Arg.Any<ManagedIdentityAuthorization>(), Arg.Any<string>(), Arg.Any<Concepts.EventStoreName?>()).Returns(WebhookTargetValidation.Allowed);
            var factory = new WebhookHttpClientFactory(provider.GetRequiredService<IHttpClientFactory>(), Substitute.For<IOAuthClient>(), tokens, policy);
            var mediator = new WebhookMediator(factory, new JsonSerializerOptions(), logger: provider.GetRequiredService<ILogger<WebhookMediator>>());
            _results.Add(await mediator.Deliver("store", new("https://receiver.example", new ManagedIdentityAuthorization("client", "api://receiver/.default"), new Dictionary<string, string>()), "partition", []));
        }
    }

    [Fact] void should_actually_attach_the_token_before_failure() => _authenticatedRequests.ShouldEqual(3);
    [Fact] void should_fail_all_deliveries() => _results.All(_ => !_.IsSuccess).ShouldBeTrue();
    [Fact] void should_keep_the_token_out_of_recordable_failure_details() => _results.ForEach(_ => _.Error!.ToString().ShouldNotContain(Token));
    [Fact] void should_capture_transport_logs() => _logs.Messages.ShouldNotBeEmpty();
    [Fact] void should_keep_the_token_out_of_log_messages() => _logs.Messages.ForEach(_ => _.ShouldNotContain(Token));

    class Receiver(HttpStatusCode status, Action received) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization!.Parameter.ShouldEqual(Token);
            received();
            return Task.FromResult(new HttpResponseMessage(status) { ReasonPhrase = Token, Content = new StringContent(Token) });
        }
    }

    class CapturedLogs : ILoggerProvider
    {
        readonly ILogger _logger = Substitute.For<ILogger>();

        public List<string> Messages => _logger.ReceivedCalls()
            .Where(_ => _.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(_ => string.Concat(_.GetArguments()[2], _.GetArguments()[3]))
            .ToList();

        public ILogger CreateLogger(string categoryName)
        {
            _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
            return _logger;
        }

        public void Dispose() { }
    }
}
