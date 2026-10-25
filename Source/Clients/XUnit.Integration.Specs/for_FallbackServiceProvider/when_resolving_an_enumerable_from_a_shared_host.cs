// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider;

public class when_resolving_an_enumerable_from_a_shared_host : Specification
{
    readonly MutableServiceRegistry _registry = new();
    readonly helper _current = new(new dependency());
    IServiceProvider _provider;
    Exception? _unregisteredError;
    Exception? _registeredError;
    IEnumerable<free> _free;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_registry);
        services.AddSingleton(new dependency());
        services.AddTransient<helper>();
        services.AddTransient<free>();
        _provider = new FallbackServiceProviderFactory(typeof(dependency).Assembly).CreateServiceProvider(services);
    }

    void Because()
    {
        _unregisteredError = Catch.Exception(() => _provider.GetRequiredService<IEnumerable<helper>>());
        _free = _provider.GetRequiredService<IEnumerable<free>>();
        _registry.Update([ServiceDescriptor.Singleton(_current)]);
        _registeredError = Catch.Exception(() => _provider.GetRequiredService<IEnumerable<helper>>());
    }

    void Destroy() => (_provider as IDisposable)?.Dispose();

    [Fact] void should_reject_enumerable_of_an_unregistered_helper_taking_dependencies() => _unregisteredError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_rejected_helper() => Assert.Contains(typeof(helper).FullName!, _unregisteredError?.Message ?? string.Empty);
    [Fact] void should_still_resolve_enumerable_of_a_dependency_free_service() => _free.Count().ShouldEqual(1);
    [Fact] void should_not_reject_once_the_helper_is_registered_for_the_current_context() => _registeredError.ShouldBeNull();

    public sealed class dependency;

    public sealed class free;

    public sealed class helper(dependency dependency)
    {
        public dependency Dependency { get; } = dependency;
    }
}
