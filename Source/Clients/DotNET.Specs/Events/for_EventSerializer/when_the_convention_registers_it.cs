// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Events.for_EventSerializer;

/// <summary>
/// The <c>IFoo -> Foo</c> convention binds <see cref="IEventSerializer"/> to <see cref="EventSerializer"/>, and the
/// integration testing package resolves it from the container to revise an event — so the lifetime the type
/// declares is the lifetime consumers get, and it is not a detail.
/// </summary>
/// <remarks>
/// It has to be scoped. It holds the <see cref="IEventTypes"/> of one event store, which is registered scoped
/// because the registry belongs to the namespace the resolving scope named — so the process lifetime would capture
/// one namespace's registry for all of them. The convention's default of transient would be safe in that respect
/// and wrong in another: every resolution rebuilds the <see cref="JsonSerializerOptions"/> and re-activates every
/// <see cref="ICanProvideAdditionalEventInformation"/>. This registers it with the lifetime the convention's own
/// rule gives it and checks both properties of scoped: one instance within a scope, a different one in the next.
/// </remarks>
public class when_the_convention_registers_it : Specification
{
    IServiceProvider _provider;
    IEventSerializer _firstInTheScope;
    IEventSerializer _secondInTheScope;
    IEventSerializer _inAnotherScope;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => Substitute.For<IEventTypes>());
        services.AddSingleton(_ => Substitute.For<IClientArtifactsProvider>());
        services.AddSingleton(_ => Substitute.For<IClientArtifactsActivator>());
        services.AddSingleton(_ => new JsonSerializerOptions());

        _ = LifetimeTheConventionGives(typeof(EventSerializer)) switch
        {
            ServiceLifetime.Singleton => services.AddSingleton<IEventSerializer, EventSerializer>(),
            ServiceLifetime.Scoped => services.AddScoped<IEventSerializer, EventSerializer>(),
            _ => services.AddTransient<IEventSerializer, EventSerializer>()
        };

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

    static ServiceLifetime LifetimeTheConventionGives(Type type)
    {
        if (Attribute.IsDefined(type, typeof(SingletonAttribute)))
        {
            return ServiceLifetime.Singleton;
        }

        return Attribute.IsDefined(type, typeof(ScopedAttribute)) ? ServiceLifetime.Scoped : ServiceLifetime.Transient;
    }

    [Fact] void should_build_the_serializer_once_per_scope() => _secondInTheScope.ShouldEqual(_firstInTheScope);
    [Fact] void should_build_a_new_serializer_for_the_next_scope() => _inAnotherScope.ShouldNotEqual(_firstInTheScope);
}
