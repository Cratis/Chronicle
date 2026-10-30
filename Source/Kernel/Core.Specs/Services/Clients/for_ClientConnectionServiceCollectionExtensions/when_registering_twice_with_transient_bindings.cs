// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Contracts.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Services.Clients.for_ClientConnectionServiceCollectionExtensions;

public class when_registering_twice_with_transient_bindings : Specification
{
    static readonly DateTimeOffset _openedAt = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
    ServiceCollection _services;
    ServiceProvider _provider;
    OpenConnections _connections;
    OpenConnections _resolvedAgain;
    OpenConnections _fromFirstScope;
    OpenConnections _fromSecondScope;
    ILocalClientConnections _localConnections;

    void Establish()
    {
        _services = new();
        _services.AddTransient<OpenConnections>();
        _services.AddTransient<ILocalClientConnections, OpenConnections>();
    }

    void Because()
    {
        _services.AddLocalClientConnections();
        _services.AddLocalClientConnections();
        _provider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _connections = _provider.GetRequiredService<OpenConnections>();
        _resolvedAgain = _provider.GetRequiredService<OpenConnections>();
        using var firstScope = _provider.CreateScope();
        using var secondScope = _provider.CreateScope();
        _fromFirstScope = firstScope.ServiceProvider.GetRequiredService<OpenConnections>();
        _fromSecondScope = secondScope.ServiceProvider.GetRequiredService<OpenConnections>();
        _localConnections = secondScope.ServiceProvider.GetRequiredService<ILocalClientConnections>();
        _fromFirstScope.Opened(new ConnectRequest { ConnectionId = "the-client", ClientVersion = "1.0.0" }, _openedAt);
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_register_one_concrete_store() => _services.Count(descriptor => descriptor.ServiceType == typeof(OpenConnections)).ShouldEqual(1);
    [Fact] void should_register_one_local_connections_binding() => _services.Count(descriptor => descriptor.ServiceType == typeof(ILocalClientConnections)).ShouldEqual(1);
    [Fact] void should_register_the_concrete_store_as_singleton() => _services.Single(descriptor => descriptor.ServiceType == typeof(OpenConnections)).Lifetime.ShouldEqual(ServiceLifetime.Singleton);
    [Fact] void should_register_local_connections_as_singleton() => _services.Single(descriptor => descriptor.ServiceType == typeof(ILocalClientConnections)).Lifetime.ShouldEqual(ServiceLifetime.Singleton);
    [Fact] void should_share_the_store_across_resolutions() => _resolvedAgain.ShouldEqual(_connections);
    [Fact] void should_share_the_store_with_the_first_scope() => _fromFirstScope.ShouldEqual(_connections);
    [Fact] void should_share_the_store_with_the_second_scope() => _fromSecondScope.ShouldEqual(_connections);
    [Fact] void should_forward_local_connections_to_the_same_store() => _localConnections.ShouldEqual(_connections);
    [Fact] void should_expose_opened_connections_to_another_store_resolution() => _fromSecondScope.GetLive(_openedAt, TimeSpan.FromSeconds(4)).Single().ConnectionId.Value.ShouldEqual("the-client");
    [Fact] void should_expose_opened_connections_to_the_local_registry() => _localConnections.GetLive(_openedAt, TimeSpan.FromSeconds(4)).Single().ConnectionId.Value.ShouldEqual("the-client");
}
