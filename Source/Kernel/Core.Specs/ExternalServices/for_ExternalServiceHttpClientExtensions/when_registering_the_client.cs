// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.ExternalServices.for_ExternalServiceHttpClientExtensions;

public class when_registering_the_client : Specification
{
    ServiceProvider _provider;
    SocketsHttpHandler _primary;
    bool _hasValidation;

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
        builder.AddExternalServiceHttpClient();
        _provider = services.BuildServiceProvider();
    }

    void Because()
    {
        var handler = _provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(ExternalServiceHttpClientFactory.HttpClientName);
        while (handler is DelegatingHandler delegating)
        {
            _hasValidation |= handler is WebhookValidationHandler;
            handler = delegating.InnerHandler;
        }
        _primary = (SocketsHttpHandler)handler;
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_disable_redirects() => _primary.AllowAutoRedirect.ShouldBeFalse();
    [Fact] void should_disable_proxies() => _primary.UseProxy.ShouldBeFalse();
    [Fact] void should_validate_connections() => _primary.ConnectCallback.ShouldNotBeNull();
    [Fact] void should_validate_requests() => _hasValidation.ShouldBeTrue();
}
