// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers;

/// <summary>
/// The three handlers that recognize bare events retain public compatibility constructors taking
/// <see cref="Events.IEventTypes"/>, but the registry belongs to one event store and is registered scoped. The
/// published singleton metadata cannot be allowed to control their runtime lifetime: explicit scoped factories
/// select the compatibility constructors with the registry belonging to that exact scope.
/// </summary>
/// <remarks>
/// The real Chronicle registration is followed by convention self-binding. That order proves the explicit
/// registration prevents convention from duplicating the published singleton binding. Scope validation proves the
/// dispatcher and concrete handlers can resolve without a captive dependency, and the old calls prove that two
/// scopes classify against two different registries.
/// </remarks>
public class when_the_container_validates_scopes : Specification
{
    readonly Type[] _handlerTypes =
    [
        typeof(EventResultHandler),
        typeof(EventsResultHandler),
        typeof(MixedSideEffectsResultHandler)
    ];

    ServiceDescriptor[] _registrations;
    object[] _resolvedHandlers;
    Exception _error;
    ServiceProvider _provider;
    bool _firstScopeResult;
    bool _secondScopeResult;
    bool[] _concreteHandlerResults;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddTypeDiscovery();
        services.AddCratisChronicleClient();
        services.AddSelfBindings();

        var registryNumber = 0;
        services.Replace(ServiceDescriptor.Scoped<Events.IEventTypes>(_ =>
        {
            var eventTypes = Substitute.For<Events.IEventTypes>();
            eventTypes.HasFor(typeof(SomeEvent)).Returns(++registryNumber == 1);
            return eventTypes;
        }));
        services.Replace(ServiceDescriptor.Transient<IInstancesOf<IReactorSideEffectHandler>>(serviceProvider =>
            new KnownInstancesOf<IReactorSideEffectHandler>([serviceProvider.GetRequiredService<EventResultHandler>()])));

        _registrations = services.Where(descriptor =>
            _handlerTypes.Contains(descriptor.ServiceType) ||
            descriptor.ServiceType == typeof(ReactorSideEffectHandlers) ||
            descriptor.ServiceType == typeof(IReactorSideEffectHandlers)).ToArray();
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    void Because() => _error = Catch.Exception(() =>
    {
        using (var firstScope = _provider.CreateScope())
        {
            _resolvedHandlers = _handlerTypes
                .Select(type => firstScope.ServiceProvider.GetRequiredService(type))
                .ToArray();
            var context = new ReactorContext(Events.EventContext.Empty, new object(), ReactorContextValues.Empty);
            _concreteHandlerResults =
            [
                ((EventResultHandler)_resolvedHandlers[0]).CanHandle(context, new SomeEvent()),
                ((EventsResultHandler)_resolvedHandlers[1]).CanHandle(context, new object[] { new SomeEvent() }),
                ((MixedSideEffectsResultHandler)_resolvedHandlers[2]).CanHandle(
                    context,
                    new object[] { new SomeEvent(), new EventForEventSourceId(Guid.NewGuid(), new SomeEvent()) })
            ];
            _firstScopeResult = firstScope.ServiceProvider
                .GetRequiredService<IReactorSideEffectHandlers>()
                .CanHandle(context, new SomeEvent());
        }

        using var secondScope = _provider.CreateScope();
        _secondScopeResult = secondScope.ServiceProvider
            .GetRequiredService<IReactorSideEffectHandlers>()
            .CanHandle(new(Events.EventContext.Empty, new object(), ReactorContextValues.Empty), new SomeEvent());
    });

    [Fact] void should_register_each_handler_and_dispatcher_contract_once() => _registrations.Length.ShouldEqual(_handlerTypes.Length + 2);
    [Fact] void should_register_every_handler_and_dispatcher_as_scoped() => _registrations.All(_ => _.Lifetime == ServiceLifetime.Scoped).ShouldBeTrue();
    [Fact] void should_resolve_without_a_captive_scoped_service() => _error.ShouldBeNull();
    [Fact] void should_resolve_every_handler() => _resolvedHandlers.Select(_ => _.GetType()).ShouldContainOnly(_handlerTypes);
    [Fact] void should_answer_the_old_call_on_every_registry_dependent_concrete_handler() => _concreteHandlerResults.All(_ => _).ShouldBeTrue();
    [Fact] void should_answer_the_old_call_from_the_first_scopes_registry() => _firstScopeResult.ShouldBeTrue();
    [Fact] void should_answer_the_old_call_from_the_second_scopes_registry() => _secondScopeResult.ShouldBeFalse();

    record SomeEvent;
}
