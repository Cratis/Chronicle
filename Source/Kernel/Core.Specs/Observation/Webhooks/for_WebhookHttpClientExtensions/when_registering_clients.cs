// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookHttpClientExtensions;

public class when_registering_clients : Specification
{
    ServiceProvider _provider;
    SocketsHttpHandler _webhook;
    SocketsHttpHandler _oauth;
    bool _pool_is_inside_resilience;

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
        _provider = services.BuildServiceProvider();
    }

    void Because()
    {
        var factory = _provider.GetRequiredService<IHttpMessageHandlerFactory>();
        var webhook = factory.CreateHandler(WebhookHttpClientFactory.HttpClientName);
        _webhook = PrimaryHandler(webhook);
        _pool_is_inside_resilience = PoolIsInsideResilience(webhook);
        _oauth = PrimaryHandler(factory.CreateHandler(OAuthClient.HttpClientName));
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_disable_webhook_redirects() => _webhook.AllowAutoRedirect.ShouldBeFalse();
    [Fact] void should_disable_oauth_redirects() => _oauth.AllowAutoRedirect.ShouldBeFalse();
    [Fact] void should_validate_webhook_connections() => _webhook.ConnectCallback.ShouldNotBeNull();
    [Fact] void should_validate_oauth_connections() => _oauth.ConnectCallback.ShouldNotBeNull();
    [Fact] void should_not_bypass_webhook_checks_through_a_proxy() => _webhook.UseProxy.ShouldBeFalse();
    [Fact] void should_not_bypass_oauth_checks_through_a_proxy() => _oauth.UseProxy.ShouldBeFalse();

    [Fact] void should_preserve_resilience_for_dedicated_connections() => _pool_is_inside_resilience.ShouldBeTrue();

    static bool PoolIsInsideResilience(HttpMessageHandler handler)
    {
        var resilienceSeen = false;
        while (handler is DelegatingHandler delegating)
        {
            resilienceSeen |= handler is Microsoft.Extensions.Http.Resilience.ResilienceHandler;
            if (handler is WebhookConnectionPoolHandler) return resilienceSeen;
            handler = delegating.InnerHandler;
        }
        return false;
    }

    static SocketsHttpHandler PrimaryHandler(HttpMessageHandler handler)
    {
        while (handler is DelegatingHandler delegating) handler = delegating.InnerHandler!;
        return (SocketsHttpHandler)handler;
    }
}
