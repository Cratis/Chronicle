// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelHostedServices;

/// <summary>
/// Finds the hosted services registered in a service collection that can reach a grain while they start.
/// </summary>
/// <remarks>
/// A Chronicle hosted service reaches grains when it, or any Chronicle dependency it is constructed with, takes an
/// <see cref="IGrainFactory"/>, an <see cref="IClusterClient"/> or a service locator. Dependencies are followed through
/// the registrations in the collection. Framework types are not followed: they are not Chronicle's to change.
/// A hosted service registered through a factory cannot be inspected, so a Chronicle factory registration counts as
/// reaching grains unless it is one of the reviewed compositions.
/// </remarks>
static class HostedServiceGrainAccess
{
    static readonly Type[] _grainAccess = [typeof(IGrainFactory), typeof(IClusterClient), typeof(IServiceProvider), typeof(IServiceScopeFactory)];

    /// <summary>
    /// The Chronicle types allowed to register a hosted service through a factory, and why.
    /// </summary>
    static readonly Type[] _reviewedFactories =
    [
        typeof(KernelHostComposition),  // Wraps the Orleans silo itself, which is what everything else waits for.
        typeof(ChronicleDataProtection) // Defers the framework's key ring warmup until the silo has started.
    ];

    /// <summary>
    /// Gets the hosted service registrations in a collection, as the type they register or the type composing them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The registered hosted service types.</returns>
    public static IEnumerable<Type> RegisteredHostedServices(IServiceCollection services) => services
        .Where(_ => _.ServiceType == typeof(IHostedService) && !_.IsKeyedService)
        .Select(_ => _.ImplementationType ?? _.ImplementationInstance?.GetType() ?? Origin(_.ImplementationFactory!.Method));

    /// <summary>
    /// Finds the hosted services that can reach a grain while they start.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The offending hosted services, by full name.</returns>
    public static IEnumerable<string> Offenders(IServiceCollection services)
    {
        foreach (var descriptor in services.Where(_ => _.ServiceType == typeof(IHostedService) && !_.IsKeyedService))
        {
            if (descriptor.ImplementationFactory is { } factory)
            {
                var origin = Origin(factory.Method);
                if (IsChronicle(origin) && !_reviewedFactories.Contains(origin)) yield return $"{origin.FullName} (factory registration)";
                continue;
            }

            var type = descriptor.ImplementationType ?? descriptor.ImplementationInstance!.GetType();
            if (IsChronicle(type) && ReachesGrains(type, services, [])) yield return type.FullName!;
        }
    }

    static bool ReachesGrains(Type type, IServiceCollection services, HashSet<Type> visited)
    {
        if (!visited.Add(type)) return false;
        return type
            .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .SelectMany(_ => _.GetParameters())
            .Select(_ => Unwrap(_.ParameterType))
            .Any(dependency =>
                _grainAccess.Any(access => access.IsAssignableFrom(dependency)) ||
                ImplementationsOf(dependency, services).Any(implementation => IsChronicle(implementation) && ReachesGrains(implementation, services, visited)));
    }

    static IEnumerable<Type> ImplementationsOf(Type dependency, IServiceCollection services)
    {
        if (dependency is { IsClass: true, IsAbstract: false }) return [dependency];
        return services
            .Where(_ => _.ServiceType == dependency && !_.IsKeyedService)
            .Select(_ => _.ImplementationType ?? _.ImplementationInstance?.GetType() ?? (_.ImplementationFactory is { } factory ? Origin(factory.Method) : null))
            .OfType<Type>();
    }

    static Type Unwrap(Type type)
    {
        if (type.IsArray) return type.GetElementType()!;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) return type.GetGenericArguments()[0];
        return type;
    }

    static Type Origin(MethodInfo method)
    {
        var type = method.DeclaringType!;
        while (type.DeclaringType is { } outer) type = outer;
        return type;
    }

    static bool IsChronicle(Type type) => type.Namespace?.StartsWith("Cratis.Chronicle", StringComparison.Ordinal) == true;
}
