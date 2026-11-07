// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.given;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookHttpClientExtensions;

public class when_the_circuit_opens_for_a_target_with_path_and_query : Specification
{
    readonly string _host = SensitiveTarget.UniqueHost();
    readonly WebhookMetricsRecorder _metrics = new();
    ServiceProvider _provider;
    HttpClient _client;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(new ChronicleOptions { Webhooks = new Cratis.Chronicle.Configuration.Webhooks { RetryDelaySeconds = 0 } }));
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        services.AddSingleton(environment);
        var builder = Substitute.For<ISiloBuilder>();
        builder.Services.Returns(services);
        builder.AddWebhookObserverHttpClient();
        services.AddHttpClient(WebhookHttpClientFactory.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => new FailingHandler());
        _provider = services.BuildServiceProvider();
        _client = _provider.GetRequiredService<IHttpClientFactory>().CreateClient(WebhookHttpClientFactory.HttpClientName);
    }

    async Task Because()
    {
        for (var i = 0; i < 12; i++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, SensitiveTarget.WithPathAndQuery(_host));
                request.Options.Set(WebhookRequestOptions.EventStore, "store");
                using var response = await _client.SendAsync(request);
            }
            catch (BrokenCircuitException)
            {
                break;
            }
        }
    }

    [Fact] void should_open_once() => _metrics.ForHost(_host).Single().Instrument.ShouldEqual("chronicle-webhook-circuit-opened");
    [Fact] void should_keep_the_authority_series() => _metrics.ForHost(_host).Single().Tags["authority"].ShouldEqual($"https://{_host}:8443");
    [Fact] void should_not_include_path_or_query() => SensitiveTarget.Leaks(_metrics.ForHost(_host).Single().Tags["authority"].ToString()).ShouldBeFalse();

    void Destroy()
    {
        _client.Dispose();
        _provider.Dispose();
        _metrics.Dispose();
    }

    class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
    }
}
