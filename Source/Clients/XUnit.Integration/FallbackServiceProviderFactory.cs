// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// An <see cref="IServiceProviderFactory{TContainerBuilder}"/> that builds the standard
/// DI container and wraps the resulting <see cref="IServiceProvider"/> with a
/// <see cref="FallbackServiceProvider"/> so that types added to
/// <see cref="MutableServiceRegistry"/> after the container was built can still be resolved.
/// </summary>
/// <remarks>
/// When a shared test host is used, services from the test assembly that are only bound in the
/// built container may be constructed from the first context's dependencies. Those services are
/// recorded on the <see cref="MutableServiceRegistry"/> so that resolving them without a registration
/// for the current context fails with an actionable error instead.
/// </remarks>
/// <param name="sharedHostTestAssembly">The test assembly whose container fallbacks must be rejected, or <see langword="null"/> when the host is not shared.</param>
internal sealed class FallbackServiceProviderFactory(Assembly? sharedHostTestAssembly = null) : IServiceProviderFactory<IServiceCollection>
{
    /// <inheritdoc/>
    public IServiceCollection CreateBuilder(IServiceCollection services) => services;

    /// <inheritdoc/>
    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
    {
        var options = new ServiceProviderOptions { ValidateOnBuild = false };
        var rejected = sharedHostTestAssembly is null ? Type.EmptyTypes : FindDependencyTakingFallbacks(containerBuilder, sharedHostTestAssembly);
        var innerProvider = containerBuilder.BuildServiceProvider(options);

        var registry = innerProvider.GetService<MutableServiceRegistry>();
        if (registry is null)
        {
            return innerProvider;
        }

        registry.RejectContainerFallbackFor(rejected);
        return new FallbackServiceProvider(innerProvider, registry);
    }

    static Type[] FindDependencyTakingFallbacks(IServiceCollection services, Assembly testAssembly)
    {
        var effective = new Dictionary<Type, ServiceDescriptor>();
        foreach (var descriptor in services.Where(descriptor => !descriptor.IsKeyedService))
        {
            effective[descriptor.ServiceType] = descriptor;
        }

        return effective.Values
            .Where(descriptor => IsFromTestAssembly(descriptor, testAssembly) && !IsDependencyFreeSelfBinding(descriptor))
            .Select(descriptor => descriptor.ServiceType)
            .ToArray();
    }

    static bool IsFromTestAssembly(ServiceDescriptor descriptor, Assembly testAssembly) =>
        descriptor.ServiceType.Assembly == testAssembly || descriptor.ImplementationType?.Assembly == testAssembly;

    static bool IsDependencyFreeSelfBinding(ServiceDescriptor descriptor) =>
        descriptor.ImplementationFactory is null &&
        descriptor.ImplementationInstance is null &&
        descriptor.ImplementationType == descriptor.ServiceType &&
        descriptor.ServiceType.GetConstructors().All(constructor => constructor.GetParameters().Length == 0);
}
