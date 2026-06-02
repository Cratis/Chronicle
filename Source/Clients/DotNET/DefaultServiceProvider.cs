// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Cratis.Traces;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle;

/// <summary>
/// Represents a default <see cref="IServiceProvider"/> that will create instances of services using the default constructor.
/// </summary>
public class DefaultServiceProvider : IServiceProvider, IServiceProviderIsService, IServiceProviderIsKeyedService, IKeyedServiceProvider, IServiceScopeFactory, IServiceScope
{
    readonly ConcurrentDictionary<Type, Type?> _conventionResolutionCache = new();

    /// <inheritdoc/>
    public IServiceProvider ServiceProvider => this;

    /// <inheritdoc/>
    public IServiceScope CreateScope() => this;

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProviderIsService)) return this;
        if (serviceType == typeof(IServiceProviderIsKeyedService)) return this;
        if (serviceType == typeof(IKeyedServiceProvider)) return this;
        if (serviceType == typeof(IServiceScopeFactory)) return this;

        // IInstancesOf<TInterface> — provide a working instance over Types.Instance so callers that
        // expect convention-based discovery (e.g. ReactorSideEffectHandlers) keep working when no
        // host DI container is in play.
        if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IInstancesOf<>))
        {
            var concrete = typeof(InstancesOf<>).MakeGenericType(serviceType.GetGenericArguments());
            return Activator.CreateInstance(concrete, Types.Types.Instance, this);
        }

        // Convention-based discovery for IFoo → Foo: the host DI container resolves this through
        // explicit registration; here we honor the same convention by finding the single concrete
        // implementer in the loaded assemblies and constructing it with recursive parameter
        // resolution. Without this, types that depend on convention-discovered services break the
        // moment a caller falls back to the DefaultServiceProvider.
        if (serviceType.IsInterface)
        {
            var concrete = ResolveConventionImplementation(serviceType);
            if (concrete is not null)
            {
                return CreateInstance(concrete);
            }
        }

        return CreateInstance(serviceType);
    }

    /// <inheritdoc/>
    public bool IsService(Type serviceType) => true;

    /// <inheritdoc/>
    public bool IsKeyedService(Type serviceType, object? serviceKey) => true;

    /// <inheritdoc/>
    public object? GetKeyedService(Type serviceType, object? serviceKey) => CreateKeyedService(serviceType, serviceKey);

    /// <inheritdoc/>
    public object GetRequiredKeyedService(Type serviceType, object? serviceKey) =>
        CreateKeyedService(serviceType, serviceKey)
            ?? throw new InvalidOperationException($"No service for type '{serviceType.FullName}' with key '{serviceKey}' has been registered.");

    static object? CreateKeyedService(Type serviceType, object? serviceKey)
    {
        // The chronicle client requests IActivitySource<T> keyed by ClientActivity.SourceName when
        // the host did not register it explicitly. Synthesize a working instance backed by a
        // System.Diagnostics.ActivitySource named after the key so client-side tracing degrades to
        // a no-op rather than throwing. Other keyed lookups fall back to default-constructor
        // activation, matching the unkeyed behavior.
        if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IActivitySource<>))
        {
            var sourceName = serviceKey as string ?? serviceType.GetGenericArguments()[0].FullName ?? serviceType.GetGenericArguments()[0].Name;
            var concrete = typeof(ActivitySource<>).MakeGenericType(serviceType.GetGenericArguments());
            return Activator.CreateInstance(concrete, new System.Diagnostics.ActivitySource(sourceName));
        }

        return Activator.CreateInstance(serviceType);
    }

    object? CreateInstance(Type type)
    {
        if (type.IsAbstract || type.IsInterface)
        {
            return null;
        }

        var constructor = type.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();
        if (constructor is null)
        {
            return Activator.CreateInstance(type);
        }

        var parameters = constructor.GetParameters();
        if (parameters.Length == 0)
        {
            return Activator.CreateInstance(type);
        }

        var arguments = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            arguments[i] = GetService(parameters[i].ParameterType);
        }

        return constructor.Invoke(arguments);
    }

    Type? ResolveConventionImplementation(Type interfaceType) =>
        _conventionResolutionCache.GetOrAdd(interfaceType, static i =>
        {
            // IFoo → Foo by name and namespace, preferring the same assembly first, then any loaded assembly.
            if (!i.Name.StartsWith('I') || i.Name.Length < 2 || !char.IsUpper(i.Name[1]))
            {
                return null;
            }

            var concreteName = i.Name[1..];
            var qualifiedName = string.IsNullOrEmpty(i.Namespace) ? concreteName : $"{i.Namespace}.{concreteName}";

            var fromSelf = i.Assembly.GetType(qualifiedName, throwOnError: false);
            if (fromSelf?.IsAbstract == false && i.IsAssignableFrom(fromSelf))
            {
                return fromSelf;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? candidate;
                try
                {
                    candidate = assembly.GetType(qualifiedName, throwOnError: false);
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }

                if (candidate?.IsAbstract == false && i.IsAssignableFrom(candidate))
                {
                    return candidate;
                }
            }

            return null;
        });
}
