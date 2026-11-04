// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using static Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider.fallback_context;

namespace Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider;

public class when_resolving_explicit_host_singletons : Specification
{
    readonly fallback_context _factory = new();
    readonly fallback_context _instance = new();
    readonly fallback_context _typeBinding = new();
    readonly fallback_context _frameworkService = new();
    readonly helper _configuredInstance = new(new tracker());
    Exception? _factoryError;
    Exception? _scopedFactoryError;
    Exception? _factoryCollectionError;
    Exception? _instanceError;
    Exception? _instanceCollectionError;
    Exception? _typeBindingError;
    ICloneable _frameworkResult;

    void Establish()
    {
        _factory.Build((services, _) => services.AddSingleton(provider => new helper(provider.GetRequiredService<tracker>())));
        _instance.Build((services, _) => services.AddSingleton(_configuredInstance));
        _typeBinding.Build((services, _) => services.AddSingleton<helper>());
        _frameworkService.Build((services, _) => services.AddSingleton<ICloneable>(provider => new host_singleton(provider.GetRequiredService<tracker>())));
        foreach (var context in Contexts()) context.Advance();
    }

    void Because()
    {
        _factoryError = Catch.Exception(() => _factory.Provider.GetRequiredService<helper>());
        using var scope = _factory.Provider.CreateScope();
        _scopedFactoryError = Catch.Exception(() => scope.ServiceProvider.GetRequiredService<helper>());
        _factoryCollectionError = Catch.Exception(() => scope.ServiceProvider.GetServices<helper>());
        _instanceError = Catch.Exception(() => _instance.Provider.GetRequiredService<helper>());
        _instanceCollectionError = Catch.Exception(() => _instance.Provider.GetServices<helper>());
        _typeBindingError = Catch.Exception(() => _typeBinding.Provider.GetRequiredService<helper>());
        _frameworkResult = _frameworkService.Provider.GetRequiredService<ICloneable>();
    }

    void Destroy()
    {
        foreach (var context in Contexts()) context.Dispose();
    }

    [Fact] void should_reject_a_test_factory_singleton_from_an_earlier_context() => _factoryError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_reject_the_factory_singleton_in_scopes() => _scopedFactoryError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_reject_the_factory_singleton_in_collections() => _factoryCollectionError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_invoke_the_earlier_context_factory() => _factory.First.Constructions.ShouldEqual(0);
    [Fact] void should_reject_a_test_instance_singleton_from_an_earlier_context() => _instanceError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_reject_the_instance_singleton_in_collections() => _instanceCollectionError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_still_reject_dependency_taking_singleton_type_bindings() => _typeBindingError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_construct_the_rejected_type_binding() => _typeBinding.First.Constructions.ShouldEqual(0);
    [Fact] void should_still_serve_a_framework_service_the_host_configured() => _frameworkResult.ShouldBeOfExactType<host_singleton>();

    fallback_context[] Contexts() => [_factory, _instance, _typeBinding, _frameworkService];

    sealed class host_singleton(tracker tracker) : ICloneable
    {
        public object Clone() => tracker;
    }
}
