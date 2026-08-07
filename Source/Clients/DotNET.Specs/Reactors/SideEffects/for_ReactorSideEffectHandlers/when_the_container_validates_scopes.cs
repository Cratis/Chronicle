// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers;

/// <summary>
/// <see cref="IEventTypes"/> is registered scoped, because the event type registry belongs to the event store —
/// and therefore the namespace — the resolving scope named. Anything Cratis's DI convention registers for the
/// process lifetime must therefore not take one in its constructor: with <c>ValidateScopes</c> on the resolution
/// is refused and the reactor's side-effect append never happens, and with it off the captured registry answers
/// for every other namespace in the host.
/// </summary>
/// <remarks>
/// Each type is registered with the lifetime its own attribute dictates, so re-introducing <c>[Singleton]</c> on
/// a type that consumes a scoped service fails here rather than at a consumer's startup.
/// </remarks>
public class when_the_container_validates_scopes : Specification
{
    static readonly Type[] _types =
    [
        typeof(EventSerializer),
        typeof(EventResultHandler),
        typeof(EventsResultHandler),
        typeof(EventForEventSourceIdResultHandler),
        typeof(EventsForEventSourceIdResultHandler),
        typeof(MixedSideEffectsResultHandler)
    ];

    IServiceCollection _services;
    Exception _error;

    void Establish()
    {
        _services = new ServiceCollection();
        _services.AddScoped(_ => Substitute.For<IEventTypes>());
        _services.AddSingleton(_ => Substitute.For<IClientArtifactsProvider>());
        _services.AddSingleton(_ => Substitute.For<IClientArtifactsActivator>());
        _services.AddSingleton(_ => new JsonSerializerOptions());

        foreach (var type in _types)
        {
            if (Attribute.IsDefined(type, typeof(SingletonAttribute)))
            {
                _services.AddSingleton(type);
            }
            else
            {
                _services.AddTransient(type);
            }
        }
    }

    void Because() => _error = Catch.Exception(() => _services.BuildServiceProvider(new ServiceProviderOptions
    {
        ValidateScopes = true,
        ValidateOnBuild = true
    }));

    [Fact] void should_build_without_a_captive_dependency() => _error.ShouldBeNull();
}
