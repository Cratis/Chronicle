// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookHttpClientExtensions;

public class when_delivering_to_different_authorities : Specification
{
    ServiceProvider _provider;
    CountingHandler _handler;
    HttpClient _firstObserver;
    HttpClient _secondObserver;
    bool _sharedCircuit;
    bool _otherHostSucceeded;
    bool _otherPortSucceeded;
    int _callsBeforeSharedRejection;
    int _callsAfterFirstDelivery;

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
        _handler = new();
        services.AddHttpClient(WebhookHttpClientFactory.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => _handler);
        _provider = services.BuildServiceProvider();
        var factory = _provider.GetRequiredService<IHttpClientFactory>();
        _firstObserver = factory.CreateClient(WebhookHttpClientFactory.HttpClientName);
        _secondObserver = factory.CreateClient(WebhookHttpClientFactory.HttpClientName);
    }

    async Task Because()
    {
        for (var i = 0; i < 12; i++)
        {
            try
            {
                using var response = await Send(_firstObserver, "https://down.invalid/first", "first-store");
                if (i == 0) _callsAfterFirstDelivery = _handler.FailingCalls;
            }
            catch (BrokenCircuitException)
            {
                break;
            }
        }

        _callsBeforeSharedRejection = _handler.FailingCalls;
        try
        {
            using var response = await Send(_secondObserver, "https://DOWN.invalid:443/second", "second-store");
        }
        catch (BrokenCircuitException)
        {
            _sharedCircuit = true;
        }

        using var otherHost = await Send(_secondObserver, "https://healthy.invalid/webhook", "second-store");
        using var otherPort = await Send(_firstObserver, "https://down.invalid:444/webhook", "first-store");
        _otherHostSucceeded = otherHost.IsSuccessStatusCode;
        _otherPortSucceeded = otherPort.IsSuccessStatusCode;
    }

    async Task<HttpResponseMessage> Send(HttpClient client, string url, string store)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Options.Set(WebhookRequestOptions.EventStore, store);
        return await client.SendAsync(request);
    }

    [Fact] void should_perform_one_http_attempt_per_delivery() => _callsAfterFirstDelivery.ShouldEqual(1);
    [Fact] void should_share_the_breaker_across_observers_paths_and_stores() => _sharedCircuit.ShouldBeTrue();
    [Fact] void should_reject_without_sending_an_http_request() => _handler.FailingCalls.ShouldEqual(_callsBeforeSharedRejection);
    [Fact] void should_isolate_another_host() => _otherHostSucceeded.ShouldBeTrue();
    [Fact] void should_isolate_another_port() => _otherPortSucceeded.ShouldBeTrue();

    void Destroy()
    {
        _firstObserver.Dispose();
        _secondObserver.Dispose();
        _provider.Dispose();
    }

    class CountingHandler : HttpMessageHandler
    {
        public int FailingCalls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var failing = request.RequestUri!.Host == "down.invalid" && request.RequestUri.Port == 443;
            if (failing) FailingCalls++;
            return Task.FromResult(new HttpResponseMessage(failing ? HttpStatusCode.InternalServerError : HttpStatusCode.OK));
        }
    }
}
