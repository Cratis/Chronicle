// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Forwards captured context services to the current mutable registry.
/// </summary>
internal static class ContextServiceForwarding
{
    /// <summary>
    /// Adds factories that resolve captured services from the current context.
    /// </summary>
    /// <remarks>
    /// The forwarder must reject stale test implementations itself because dependencies resolved
    /// inside the built container bypass the fallback provider's policy check.
    /// </remarks>
    /// <param name="services">The host service collection.</param>
    /// <param name="contextServices">The captured context registrations.</param>
    /// <param name="sharedHostTestAssembly">The test assembly for a shared host, or <see langword="null"/> for a non-shared host.</param>
    /// <exception cref="InvalidOperationException">A forwarder resolves a test service missing from the current shared-host context.</exception>
    public static void AddContextServiceForwarders(this IServiceCollection services, IEnumerable<ServiceDescriptor> contextServices, Assembly? sharedHostTestAssembly)
    {
        foreach (var descriptor in contextServices)
        {
            var requiresCurrent = sharedHostTestAssembly is not null &&
                ContainerFallbackPolicy.RequiresCurrentRegistration(descriptor, sharedHostTestAssembly);
            services.AddTransient(descriptor.ServiceType, sp =>
            {
                var registry = sp.GetRequiredService<MutableServiceRegistry>();
                return registry.TryGet(descriptor.ServiceType, sp)
                    ?? (requiresCurrent
                        ? throw ContainerFallbackPolicy.CreateRejection(descriptor.ServiceType)
                        : ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType ?? descriptor.ServiceType));
            });
        }
    }
}
