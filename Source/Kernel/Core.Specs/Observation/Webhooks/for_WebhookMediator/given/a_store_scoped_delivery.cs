// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.given;

public class a_store_scoped_delivery : a_webhook_mediator
{
    protected WebhookTarget _target;
    protected Receiver _receiver;
    HttpClient _client;

    void Establish()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        var options = new ChronicleOptions
        {
            Webhooks = new()
            {
                Validation = WebhookValidationMode.Enforce,
                EventStoreAllowedHosts = new Dictionary<string, string[]> { ["orders"] = ["receiver.example"] }
            }
        };
        var policy = new WebhookTargetPolicy(Options.Create(options), environment, Substitute.For<IWebhookHostResolver>(), NullLogger<WebhookTargetPolicy>.Instance);
        _receiver = new();
        _client = new(new WebhookValidationHandler(policy, NullLogger<WebhookValidationHandler>.Instance) { InnerHandler = _receiver })
        {
            BaseAddress = new("https://receiver.example")
        };
        _target = new("https://receiver.example", WebhookAuthorization.None, new Dictionary<string, string>());
        _httpClientFactory.Create(Arg.Any<WebhookTarget>(), Arg.Any<string?>()).Returns(_client);
    }

    void Destroy() => _client.Dispose();

    protected sealed class Receiver : HttpMessageHandler
    {
        public List<EventStoreName?> Stores { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Options.TryGetValue(WebhookRequestOptions.EventStore, out var store);
            Stores.Add(store);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
