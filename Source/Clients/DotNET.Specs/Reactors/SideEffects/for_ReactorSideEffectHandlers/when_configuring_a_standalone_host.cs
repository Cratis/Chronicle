// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers;

public class when_configuring_a_standalone_host : Specification
{
    ServiceProvider _provider;
    IReactorContextValuesProvider[] _providers;
    IReactorSideEffectHandler[] _handlers;
    ReactorContextValues _values;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddCratisChronicleClient();
        _provider = services.BuildServiceProvider();
    }

    void Because()
    {
        _providers = new[]
        {
            typeof(EventSourceIdValuesProvider), typeof(EventSourceTypeValuesProvider),
            typeof(EventStreamIdValuesProvider), typeof(EventStreamTypeValuesProvider), typeof(SubjectValuesProvider)
        }.Select(type => (IReactorContextValuesProvider?)_provider.GetService(type)).OfType<IReactorContextValuesProvider>().ToArray();
        _values = new ReactorContextValuesBuilder(new InstancesOf<IReactorContextValuesProvider>(TypeUniverse.For(_provider), _provider))
            .Build(new object(), EventContext.EmptyWithEventSourceId(EventSourceId.New()));
        _handlers = new[]
        {
            typeof(EventForEventSourceIdResultHandler), typeof(EventsForEventSourceIdResultHandler),
            typeof(EventsWithConcurrencyScopesResultHandler)
        }.Select(type => (IReactorSideEffectHandler?)_provider.GetService(type)).OfType<IReactorSideEffectHandler>().ToArray();
    }

    [Fact] void should_register_all_context_value_providers() => _providers.Length.ShouldEqual(5);
    [Fact] void should_build_context_values_from_discovered_providers() => _values.ShouldNotBeNull();
    [Fact] void should_register_all_client_side_effect_handlers() => _handlers.Length.ShouldEqual(3);
}
