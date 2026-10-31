// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider;

public class when_resolving_from_a_shared_host : Specification
{
    readonly MutableServiceRegistry _registry = new();
    readonly dependency _current = new();
    IServiceProvider _provider;
    dependency_free _selfBound;
    unregistered_consumer _unregistered;
    ILoggerFactory _framework;
    scoped_lifetime _scoped;
    Exception? _helperError;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_registry);
        services.AddTransient<dependency_free>();
        services.AddTransient<dependency_taking_helper>();
        services.AddSingleton(new dependency());
        services.AddScoped<scoped_lifetime>();
        _provider = new FallbackServiceProviderFactory(typeof(dependency).Assembly).CreateServiceProvider(services);
        _registry.Update([ServiceDescriptor.Singleton(_current)]);
    }

    async Task Because()
    {
        _selfBound = _provider.GetRequiredService<dependency_free>();
        _unregistered = ActivatorUtilities.GetServiceOrCreateInstance<unregistered_consumer>(_provider);
        _framework = _provider.GetRequiredService<ILoggerFactory>();
        _helperError = Catch.Exception(() => _provider.GetRequiredService<dependency_taking_helper>());
        var scope = ((IServiceScopeFactory)_provider).CreateScope();
        _scoped = scope.ServiceProvider.GetRequiredService<scoped_lifetime>();
        await ((IAsyncDisposable)scope).DisposeAsync();
    }

    void Destroy() => (_provider as IDisposable)?.Dispose();

    [Fact] void should_construct_dependency_free_self_binding() => _selfBound.ShouldNotBeNull();
    [Fact] void should_construct_an_unregistered_type_with_the_current_registration() => _unregistered.Dependency.ShouldBeSame(_current);
    [Fact] void should_resolve_framework_services_from_the_container() => _framework.ShouldNotBeNull();
    [Fact] void should_reject_a_registered_test_helper_taking_dependencies() => _helperError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_rejected_helper() => Assert.Contains(typeof(dependency_taking_helper).FullName!, _helperError?.Message ?? string.Empty);
    [Fact] void should_dispose_the_scope_asynchronously() => _scoped.IsDisposed.ShouldBeTrue();

    public sealed class dependency;

    public sealed class dependency_free;

    public sealed class dependency_taking_helper(dependency dependency)
    {
        public dependency Dependency { get; } = dependency;
    }

    public sealed class unregistered_consumer(dependency dependency)
    {
        public dependency Dependency { get; } = dependency;
    }

    public sealed class scoped_lifetime : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
