// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle;

/// <summary>
/// Resolves the set of types the client discovers artifacts and providers from.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Cratis.Types.Types.Instance"/> and <see cref="Cratis.Serialization.DerivedTypes.Instance"/>
/// are static fields, so each is a snapshot of the generated type-discovery registry taken the first time
/// anything touches it. Generated providers register from module initializers that only run once the
/// assembly closure walk reaches their assembly, and that walk happens after such a first touch - so a
/// client reading those statics can hold a universe missing every provider the walk brings in later. It
/// fails silently: a shorter <see cref="ITypes.FindMultiple{T}"/> result is indistinguishable from a
/// feature nobody wrote.
/// </para>
/// <para>
/// A host wired through dependency injection has an <see cref="ITypes"/> in its container, and that one is
/// authoritative for it. A bare <see cref="ChronicleClient"/> has no container to ask and takes Fundamentals'
/// current default universe instead - the same instance <c>AddTypeDiscovery()</c> registers, rebuilt when
/// the registered provider set grows.
/// </para>
/// </remarks>
internal static class TypeUniverse
{
    static CachedDerivedTypes? _derivedTypes;

    /// <summary>
    /// Gets the current default type universe, outside any container.
    /// </summary>
    internal static ITypes Current => TypesServiceCollectionExtensions.CurrentTypeUniverse();

    /// <summary>
    /// Gets the type universe for a service provider - the one in its container when it has one, and the
    /// current default universe when it does not.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to get the universe for.</param>
    /// <returns>The <see cref="ITypes"/> to discover from.</returns>
    internal static ITypes For(IServiceProvider serviceProvider)
    {
        // DefaultServiceProvider answers IsService with true for every type and activates by default
        // constructor, so asking it for an interface throws rather than resolving. It is the "no container"
        // case by construction, so it is excluded here rather than probed.
        if (serviceProvider is not DefaultServiceProvider &&
            serviceProvider.GetService(typeof(IServiceProviderIsService)) is IServiceProviderIsService serviceProviderIsService &&
            serviceProviderIsService.IsService(typeof(ITypes)) &&
            serviceProvider.GetService(typeof(ITypes)) is ITypes types)
        {
            return types;
        }

        return Current;
    }

    /// <summary>
    /// Gets the <see cref="IDerivedTypes"/> for the current default type universe.
    /// </summary>
    /// <returns>The <see cref="IDerivedTypes"/> to resolve polymorphic types with.</returns>
    /// <remarks>
    /// Building one walks every discovered type, so the result is held for as long as the universe it was
    /// built from is the current one - which is for the rest of the process once the provider set settles.
    /// </remarks>
    internal static IDerivedTypes CurrentDerivedTypes()
    {
        var types = Current;
        var cached = Volatile.Read(ref _derivedTypes);
        if (cached is not null && ReferenceEquals(cached.Universe, types))
        {
            return cached.DerivedTypes;
        }

        var derivedTypes = new DerivedTypes(types);
        Volatile.Write(ref _derivedTypes, new CachedDerivedTypes(types, derivedTypes));
        return derivedTypes;
    }

    sealed record CachedDerivedTypes(ITypes Universe, IDerivedTypes DerivedTypes);
}
