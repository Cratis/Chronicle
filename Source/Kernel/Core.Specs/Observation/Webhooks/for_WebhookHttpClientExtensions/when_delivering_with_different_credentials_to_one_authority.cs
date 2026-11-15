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

public class when_delivering_with_different_credentials_to_one_authority : Specification
{
    readonly string _host = SensitiveTarget.UniqueHost();
    ServiceProvider _provider;
    CountingHandler _handler;
    HttpClient _firstObserver;
    HttpClient _secondObserver;
    bool _sharedCircuit;
    bool _otherPortSucceeded;
    int _callsBeforeSharedRejection;
    int _callsAfterSharedRejection;

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
                using var response = await Send(_firstObserver, $"https://u1:s1@{_host}:8443/a?x");
            }
            catch (BrokenCircuitException)
            {
                break;
            }
        }

        _callsBeforeSharedRejection = _handler.FailingCalls;
        try
        {
            using var response = await Send(_secondObserver, $"https://u2:s2@{_host}:8443/b?y");
        }
        catch (BrokenCircuitException)
        {
            _sharedCircuit = true;
        }
        _callsAfterSharedRejection = _handler.FailingCalls;
        using var otherPort = await Send(_firstObserver, $"https://{_host}:8444/");
        _otherPortSucceeded = otherPort.IsSuccessStatusCode;
    }

    static async Task<HttpResponseMessage> Send(HttpClient client, string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Options.Set(WebhookRequestOptions.EventStore, "store");
        return await client.SendAsync(request);
    }

    [Fact] void should_share_the_circuit_across_credentials() => _sharedCircuit.ShouldBeTrue();
    [Fact] void should_reject_without_sending() => _callsAfterSharedRejection.ShouldEqual(_callsBeforeSharedRejection);
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
            var failing = request.RequestUri!.Port == 8443;
            if (failing) FailingCalls++;
            return Task.FromResult(new HttpResponseMessage(failing ? HttpStatusCode.InternalServerError : HttpStatusCode.OK));
        }
    }
}
