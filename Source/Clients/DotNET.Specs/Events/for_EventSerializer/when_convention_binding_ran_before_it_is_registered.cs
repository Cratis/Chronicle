// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Events.for_EventSerializer;

/// <summary>
/// Convention binding, as Cratis Arc runs it before the Chronicle client is added, registers the serializer first.
/// Chronicle's registration must replace that binding, otherwise the serializer would be built for the root
/// provider and consume the scoped <see cref="IEventTypes"/> registry.
/// </summary>
/// <remarks>
/// A singleton convention binding stands in for the earlier registration, whatever the lifetime the convention picks.
/// Scope validation is on, so resolving a serializer that captured the scoped registry throws.
/// </remarks>
public class when_convention_binding_ran_before_it_is_registered : Specification
{
    IServiceProvider _provider;
    IEventSerializer _firstInTheScope;
    IEventSerializer _secondInTheScope;
    IEventSerializer _inAnotherScope;
    ServiceDescriptor[] _serializerRegistrations;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => Substitute.For<IEventTypes>());
        services.AddSingleton(_ => Substitute.For<IClientArtifactsProvider>());
        services.AddSingleton(_ => Substitute.For<IClientArtifactsActivator>());
        services.AddSingleton(_ => new JsonSerializerOptions());
        services.AddTypeDiscovery();
        services.AddSingleton<EventSerializer>();
        services.AddSingleton<IEventSerializer, EventSerializer>();
        services.AddBindingsByConvention();
        services.AddSelfBindings();
        services.AddEventSerializer();

        _serializerRegistrations = services
            .Where(descriptor => descriptor.ServiceType == typeof(EventSerializer) || descriptor.ServiceType == typeof(IEventSerializer))
            .ToArray();

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    void Because()
    {
        using (var scope = _provider.CreateScope())
        {
            _firstInTheScope = scope.ServiceProvider.GetRequiredService<IEventSerializer>();
            _secondInTheScope = scope.ServiceProvider.GetRequiredService<IEventSerializer>();
        }

        using var anotherScope = _provider.CreateScope();
        _inAnotherScope = anotherScope.ServiceProvider.GetRequiredService<IEventSerializer>();
    }

    [Fact] void should_register_only_the_explicit_scoped_services() => _serializerRegistrations.All(_ => _.Lifetime == ServiceLifetime.Scoped).ShouldBeTrue();
    [Fact] void should_register_the_concrete_and_contract_once_each() => _serializerRegistrations.Length.ShouldEqual(2);
    [Fact] void should_build_the_serializer_once_per_scope() => _secondInTheScope.ShouldEqual(_firstInTheScope);
    [Fact] void should_build_a_new_serializer_for_the_next_scope() => _inAnotherScope.ShouldNotEqual(_firstInTheScope);
}
