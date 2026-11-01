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
    readonly helper _configuredInstance = new(new tracker());
    helper _factoryResult;
    helper _scopedResult;
    helper[] _factoryCollection;
    helper _instanceResult;
    helper[] _instanceCollection;
    Exception? _typeBindingError;

    void Establish()
    {
        _factory.Build((services, _) => services.AddSingleton(provider => new helper(provider.GetRequiredService<tracker>())));
        _instance.Build((services, _) => services.AddSingleton(_configuredInstance));
        _typeBinding.Build((services, _) => services.AddSingleton<helper>());
        _factory.Advance();
        _instance.Advance();
        _typeBinding.Advance();
    }

    void Because()
    {
        _factoryResult = _factory.Provider.GetRequiredService<helper>();
        using var scope = _factory.Provider.CreateScope();
        _scopedResult = scope.ServiceProvider.GetRequiredService<helper>();
        _factoryCollection = scope.ServiceProvider.GetServices<helper>().ToArray();
        _instanceResult = _instance.Provider.GetRequiredService<helper>();
        _instanceCollection = _instance.Provider.GetServices<helper>().ToArray();
        _typeBindingError = Catch.Exception(() => _typeBinding.Provider.GetRequiredService<helper>());
    }

    void Destroy()
    {
        _factory.Dispose();
        _instance.Dispose();
        _typeBinding.Dispose();
    }

    [Fact] void should_preserve_the_explicit_host_factory_dependencies() => _factoryResult.Tracker.ShouldBeSame(_factory.First);
    [Fact] void should_preserve_factory_singleton_identity_in_scopes() => _scopedResult.ShouldBeSame(_factoryResult);
    [Fact] void should_preserve_factory_singleton_identity_in_collections() => _factoryCollection.Single().ShouldBeSame(_factoryResult);
    [Fact] void should_invoke_the_host_factory_only_once() => _factory.First.Constructions.ShouldEqual(1);
    [Fact] void should_preserve_the_explicit_host_instance() => _instanceResult.ShouldBeSame(_configuredInstance);
    [Fact] void should_preserve_instance_identity_in_collections() => _instanceCollection.Single().ShouldBeSame(_configuredInstance);
    [Fact] void should_still_reject_dependency_taking_singleton_type_bindings() => _typeBindingError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_construct_the_rejected_singleton() => _typeBinding.First.Constructions.ShouldEqual(0);
}
