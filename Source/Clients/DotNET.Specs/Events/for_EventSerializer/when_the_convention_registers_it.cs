// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Events.for_EventSerializer;

/// <summary>
/// The published <see cref="EventSerializer"/> metadata said singleton. Chronicle keeps that metadata for binary
/// and reflection compatibility, but explicitly binds the runtime service once per event-store scope before
/// convention binding runs.
/// </summary>
/// <remarks>
/// The explicit registration must win over convention self-binding: one scope gets one serializer and a second
/// scope gets another, so neither namespace's <see cref="IEventTypes"/> registry can be captured by the other.
/// </remarks>
public class when_the_convention_registers_it : Specification
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
        services.AddEventSerializer();
        services.AddSelfBindings();

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

    [Fact] void should_retain_the_published_singleton_metadata() => Attribute.IsDefined(typeof(EventSerializer), typeof(SingletonAttribute)).ShouldBeTrue();
    [Fact] void should_register_only_the_explicit_scoped_services() => _serializerRegistrations.All(_ => _.Lifetime == ServiceLifetime.Scoped).ShouldBeTrue();
    [Fact] void should_register_the_concrete_and_contract_once_each() => _serializerRegistrations.Length.ShouldEqual(2);
    [Fact] void should_build_the_serializer_once_per_scope() => _secondInTheScope.ShouldEqual(_firstInTheScope);
    [Fact] void should_build_a_new_serializer_for_the_next_scope() => _inAnotherScope.ShouldNotEqual(_firstInTheScope);
}
