// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider;

public class when_resolving_an_open_generic_from_a_shared_host : Specification
{
    readonly MutableServiceRegistry _registry = new();
    IServiceProvider _provider;
    Exception? _dependencyError;
    generic_free<int> _free;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_registry);
        services.AddSingleton(new dependency());
        services.AddTransient(typeof(generic_with_dependency<>));
        services.AddTransient(typeof(generic_free<>));
        _provider = new FallbackServiceProviderFactory(typeof(dependency).Assembly).CreateServiceProvider(services);
    }

    void Because()
    {
        _dependencyError = Catch.Exception(() => _provider.GetRequiredService<generic_with_dependency<int>>());
        _free = _provider.GetRequiredService<generic_free<int>>();
    }

    void Destroy() => (_provider as IDisposable)?.Dispose();

    [Fact] void should_reject_the_closed_generic_taking_dependencies() => _dependencyError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_rejected_generic() => Assert.Contains("generic_with_dependency", _dependencyError?.Message ?? string.Empty);
    [Fact] void should_still_construct_a_dependency_free_generic() => _free.ShouldNotBeNull();

    public sealed class dependency;

    public sealed class generic_with_dependency<T>(dependency dependency)
    {
        public dependency Dependency { get; } = dependency;
    }

    public sealed class generic_free<T>;
}
