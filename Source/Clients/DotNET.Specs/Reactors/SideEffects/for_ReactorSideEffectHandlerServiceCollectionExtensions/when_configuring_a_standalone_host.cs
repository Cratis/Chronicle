// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlerServiceCollectionExtensions;

public class when_configuring_a_standalone_host : Specification
{
    ServiceProvider _provider;
    Type[] _providerTypes;
    Type[] _handlerTypes;
    Type[] _registeredHandlerTypes;
    ReactorContextValues _values;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddCratisChronicleClient();
        _registeredHandlerTypes = services.Select(descriptor => descriptor.ServiceType).ToArray();
        _provider = services.BuildServiceProvider();
        var assemblyTypes = typeof(ReactorSideEffectHandlers).Assembly.GetTypes();
        _providerTypes = assemblyTypes.Where(type => type.IsClass && !type.IsAbstract && typeof(IReactorContextValuesProvider).IsAssignableFrom(type)).ToArray();
        _handlerTypes = assemblyTypes.Where(type => type.IsClass && !type.IsAbstract && typeof(IReactorSideEffectHandler).IsAssignableFrom(type)).ToArray();
        _values = new ReactorContextValuesBuilder(new InstancesOf<IReactorContextValuesProvider>(TypeUniverse.For(_provider), _provider))
            .Build(new object(), EventContext.EmptyWithEventSourceId(EventSourceId.New()));
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_register_all_context_value_providers() => _providerTypes.All(type => _provider.GetService(type) is IReactorContextValuesProvider).ShouldBeTrue();
    [Fact] void should_build_context_values_from_discovered_providers() => _values.ShouldNotBeNull();
    [Fact] void should_register_all_client_side_effect_handlers() => _handlerTypes.All(_registeredHandlerTypes.Contains).ShouldBeTrue();
}
