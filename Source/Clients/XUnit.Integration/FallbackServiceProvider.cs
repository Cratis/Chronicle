// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Wraps an inner <see cref="IServiceProvider"/> and falls back to
/// <see cref="MutableServiceRegistry"/> for types that are not registered
/// in the built DI container but were added by a test fixture at runtime.
/// </summary>
/// <param name="inner">The inner <see cref="IServiceProvider"/> built from the DI container.</param>
/// <param name="registry">The <see cref="MutableServiceRegistry"/> to fall back to.</param>
internal sealed class FallbackServiceProvider(IServiceProvider inner, MutableServiceRegistry registry)
    : IServiceProvider, ISupportRequiredService, IServiceScopeFactory,
      IKeyedServiceProvider, IServiceProviderIsKeyedService, IServiceProviderIsService,
      IDisposable, IAsyncDisposable
{
    /// <inheritdoc/>
    public object? GetService(Type serviceType)
    {
        // Return ourselves for framework meta-types so scoped resolution also goes through the wrapper.
        if (serviceType == typeof(IServiceProvider))
        {
            return this;
        }

        if (serviceType == typeof(IServiceScopeFactory))
        {
            return this;
        }

        if (serviceType == typeof(IKeyedServiceProvider))
        {
            return this;
        }

        if (serviceType == typeof(IServiceProviderIsKeyedService))
        {
            return this;
        }

        if (serviceType == typeof(IServiceProviderIsService))
        {
            return this;
        }

        // Check the mutable registry FIRST — it always has the most up-to-date
        // test instances. When multiple tests share the same type (e.g. SomeReactor),
        // the first test's instance lives in the inner container, but subsequent tests
        // replace it only in the registry. Checking the registry first ensures the
        // current test's instance is always returned.
        var registryResult = registry.TryGet(serviceType, this);
        if (registryResult is not null)
        {
            return registryResult;
        }

        // Fall back to the inner (built) container for framework and silo types.
        return inner.GetService(serviceType);
    }

    /// <inheritdoc/>
    public object GetRequiredService(Type serviceType)
    {
        return GetService(serviceType)
            ?? throw new InvalidOperationException($"No service for type '{serviceType}' has been registered.");
    }

    /// <inheritdoc/>
    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        if (inner is IKeyedServiceProvider keyedProvider)
        {
            return keyedProvider.GetKeyedService(serviceType, serviceKey);
        }

        return null;
    }

    /// <inheritdoc/>
    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        if (inner is IKeyedServiceProvider keyedProvider)
        {
            return keyedProvider.GetRequiredKeyedService(serviceType, serviceKey);
        }

        throw new InvalidOperationException(
            $"No keyed service for type '{serviceType}' with key '{serviceKey}' has been registered.");
    }

    /// <inheritdoc/>
    public bool IsKeyedService(Type serviceType, object? serviceKey)
    {
        if (inner is IServiceProviderIsKeyedService isKeyedService)
        {
            return isKeyedService.IsKeyedService(serviceType, serviceKey);
        }

        return false;
    }

    /// <inheritdoc/>
    public bool IsService(Type serviceType)
    {
        if (inner is IServiceProviderIsService isService && isService.IsService(serviceType))
        {
            return true;
        }

        return registry.HasService(serviceType);
    }

    /// <inheritdoc/>
    public IServiceScope CreateScope()
    {
        var innerFactory = inner as IServiceScopeFactory
            ?? inner.GetRequiredService<IServiceScopeFactory>();
        var scope = innerFactory.CreateScope();
        return new FallbackServiceScope(scope, registry);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (inner is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (inner is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
            return;
        }

        if (inner is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
