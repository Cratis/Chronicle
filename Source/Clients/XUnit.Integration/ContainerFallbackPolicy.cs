// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Immutable classification of built-container fallbacks, without retaining registrations or instances.
/// </summary>
/// <remarks>
/// Direct requests use the last exact registration before any open-generic fallback. Synthesized
/// collections include every contributor. Unsafe open-generic collection contributors are rejected
/// conservatively, even when their constraints would make DI skip them for a particular closed type.
/// Explicit collection registrations are direct selections, not synthesized collections.
/// </remarks>
internal sealed class ContainerFallbackPolicy
{
    readonly FrozenDictionary<Type, bool> _singleSelections;
    readonly FrozenSet<Type> _unsafeCollectionContributors;

    ContainerFallbackPolicy(Dictionary<Type, bool> singleSelections, HashSet<Type> unsafeCollectionContributors)
    {
        _singleSelections = singleSelections.ToFrozenDictionary();
        _unsafeCollectionContributors = unsafeCollectionContributors.ToFrozenSet();
    }

    /// <summary>
    /// Gets the policy for a host that is not shared between test contexts.
    /// </summary>
    public static ContainerFallbackPolicy Empty { get; } = new([], []);

    /// <summary>
    /// Classifies the final unkeyed descriptors of a shared test host.
    /// </summary>
    /// <param name="descriptors">The final container registrations.</param>
    /// <param name="testAssembly">The assembly defining test services.</param>
    /// <returns>The immutable fallback policy.</returns>
    public static ContainerFallbackPolicy Create(IEnumerable<ServiceDescriptor> descriptors, Assembly testAssembly)
    {
        var selections = new Dictionary<Type, bool>();
        var contributors = new HashSet<Type>();
        foreach (var descriptor in descriptors.Where(descriptor => !descriptor.IsKeyedService))
        {
            var unsafeFallback = IsProtected(descriptor, testAssembly) && !IsDependencyFreeSelfBinding(descriptor);
            selections[descriptor.ServiceType] = unsafeFallback;
            if (unsafeFallback)
            {
                contributors.Add(descriptor.ServiceType);
            }
        }
        return new(selections, contributors);
    }

    /// <summary>
    /// Determines whether the built container must not resolve a wrapper-visible request.
    /// </summary>
    /// <param name="serviceType">The requested service type.</param>
    /// <returns>Whether the fallback could use dependencies captured by the first context.</returns>
    public bool Rejects(Type serviceType)
    {
        if (_singleSelections.TryGetValue(serviceType, out var unsafeSelection))
        {
            return unsafeSelection;
        }
        if (!serviceType.IsConstructedGenericType)
        {
            return false;
        }
        if (_singleSelections.TryGetValue(serviceType.GetGenericTypeDefinition(), out unsafeSelection))
        {
            return unsafeSelection;
        }
        return serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
            HasUnsafeContributor(serviceType.GetGenericArguments()[0]);
    }

    static bool IsProtected(ServiceDescriptor descriptor, Assembly testAssembly) =>
        IsProtectedType(descriptor.ServiceType, testAssembly) || descriptor.ImplementationType?.Assembly == testAssembly;

    static bool IsProtectedType(Type type, Assembly testAssembly) =>
        type.Assembly == testAssembly ||
        (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
            IsProtectedType(type.GetGenericArguments()[0], testAssembly));

    static bool IsDependencyFreeSelfBinding(ServiceDescriptor descriptor) =>
        descriptor.ImplementationFactory is null &&
        descriptor.ImplementationInstance is null &&
        descriptor.ImplementationType == descriptor.ServiceType &&
        descriptor.ServiceType.GetConstructors().All(constructor => constructor.GetParameters().Length == 0);

    bool HasUnsafeContributor(Type elementType) =>
        _unsafeCollectionContributors.Contains(elementType) ||
        (elementType.IsConstructedGenericType && _unsafeCollectionContributors.Contains(elementType.GetGenericTypeDefinition()));
}
