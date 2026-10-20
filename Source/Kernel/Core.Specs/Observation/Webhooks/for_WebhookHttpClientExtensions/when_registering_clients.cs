// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookHttpClientExtensions;

public class when_registering_clients : Specification
{
    ServiceProvider _provider;
    SocketsHttpHandler _webhook;
    SocketsHttpHandler _oauth;

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
        _webhook = PrimaryHandler(factory.CreateHandler(WebhookHttpClientFactory.HttpClientName));
        _oauth = PrimaryHandler(factory.CreateHandler(OAuthClient.HttpClientName));
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_disable_webhook_redirects() => _webhook.AllowAutoRedirect.ShouldBeFalse();
    [Fact] void should_disable_oauth_redirects() => _oauth.AllowAutoRedirect.ShouldBeFalse();
    [Fact] void should_validate_webhook_connections() => _webhook.ConnectCallback.ShouldNotBeNull();
    [Fact] void should_validate_oauth_connections() => _oauth.ConnectCallback.ShouldNotBeNull();
    [Fact] void should_not_bypass_webhook_checks_through_a_proxy() => _webhook.UseProxy.ShouldBeFalse();
    [Fact] void should_not_bypass_oauth_checks_through_a_proxy() => _oauth.UseProxy.ShouldBeFalse();

    static SocketsHttpHandler PrimaryHandler(HttpMessageHandler handler)
    {
        while (handler is DelegatingHandler delegating) handler = delegating.InnerHandler!;
        return (SocketsHttpHandler)handler;
    }
}
