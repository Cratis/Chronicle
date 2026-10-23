// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookHttpClientExtensions;

public class when_responses_do_not_indicate_an_unavailable_target : Specification
{
    ServiceProvider _provider;
    HttpClient _client;
    Receiver _receiver;
    int _matchingResponses;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(new ChronicleOptions()));
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        services.AddSingleton(environment);
        var builder = Substitute.For<ISiloBuilder>();
        builder.Services.Returns(services);
        builder.AddWebhookObserverHttpClient();
        _receiver = new();
        services.AddHttpClient(WebhookHttpClientFactory.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => _receiver);
        _provider = services.BuildServiceProvider();
        _client = _provider.GetRequiredService<IHttpClientFactory>().CreateClient(WebhookHttpClientFactory.HttpClientName);
    }

    async Task Because()
    {
        foreach (var status in new[] { 429, 503, 403 })
        {
            for (var i = 0; i < 16; i++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"https://status-{status}.invalid/{status}");
                using var response = await _client.SendAsync(request);
                if ((int)response.StatusCode == status) _matchingResponses++;
            }
        }
    }

    [Fact] void should_not_trip_the_breaker_for_rate_limits_backpressure_or_configuration_errors() => _matchingResponses.ShouldEqual(48);
    [Fact] void should_send_once_per_delivery() => _receiver.Calls.ShouldEqual(48);

    void Destroy()
    {
        _client.Dispose();
        _provider.Dispose();
        _receiver.Dispose();
    }

    sealed class Receiver : HttpMessageHandler
    {
        readonly List<HttpResponseMessage> _responses = [];
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var status = (HttpStatusCode)int.Parse(request.RequestUri!.AbsolutePath[1..]);
            var response = new HttpResponseMessage(status);
            response.Headers.TryAddWithoutValidation("Retry-After", "120");
            _responses.Add(response);
            return Task.FromResult(response);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var response in _responses) response.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
