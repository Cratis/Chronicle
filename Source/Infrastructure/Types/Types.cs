// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Collections;

namespace Cratis.Types;

/// <summary>
/// Represents an implementation of <see cref="ITypes"/>.
/// </summary>
public class Types : ITypes
{
    /// <summary>
    /// Gets the global instance of <see cref="Types"/>.
    /// </summary>
    /// <remarks>
    /// Its recommended to use the singleton defined here, rather than building your own instance.
    /// This is due to the performance impact of scanning all assemblies in the application.
    /// </remarks>
    public static readonly Types Instance = new();

    readonly IContractToImplementorsMap _contractToImplementorsMap = new ContractToImplementorsMap();
    readonly List<Assembly> _assemblies = [];

    /// <summary>
    /// Initializes a new instance of <see cref="Types"/>.
    /// </summary>
    /// <remarks>
    /// This will automatically set up <see cref="Types"/> using the <see cref="ProjectReferencedAssemblies"/> and <see cref="PackageReferencedAssemblies"/> providers.
    /// </remarks>
    public Types()
        : this(new ICanProvideAssembliesForDiscovery[]
        {
            ProjectReferencedAssemblies.Instance,
            PackageReferencedAssemblies.Instance
        })
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Types"/>.
    /// </summary>
    /// <param name="assemblyProviders">Collection of assembly providers.</param>
    public Types(IEnumerable<ICanProvideAssembliesForDiscovery> assemblyProviders)
    {
        assemblyProviders.ForEach(_ => _.Initialize());
        var assemblies = assemblyProviders.SelectMany(_ => _.Assemblies).Distinct();
        _assemblies.AddRange(assemblies);
        All = _assemblies.SelectMany(_ => _.DefinedTypes).Distinct().ToArray();
        _contractToImplementorsMap.Feed(All);
    }

    /// <inheritdoc/>
    public IEnumerable<Assembly> Assemblies => _assemblies;

    /// <inheritdoc/>
    public IEnumerable<Type> All { get; }

    /// <inheritdoc/>
    public Type FindSingle<T>() => FindSingle(typeof(T));

    /// <inheritdoc/>
    public IEnumerable<Type> FindMultiple<T>() => FindMultiple(typeof(T));

    /// <inheritdoc/>
    public Type FindSingle(Type type)
    {
        var typesFound = _contractToImplementorsMap.GetImplementorsFor(type);
        ThrowIfMultipleTypesFound(type, typesFound);
        return typesFound.SingleOrDefault()!;
    }

    /// <inheritdoc/>
    public IEnumerable<Type> FindMultiple(Type type)
        => _contractToImplementorsMap.GetImplementorsFor(type);

    /// <inheritdoc/>
    public Type FindTypeByFullName(string fullName)
    {
        var typeFound = _contractToImplementorsMap.All.SingleOrDefault(t => t.FullName == fullName);
        ThrowIfTypeNotFound(fullName, typeFound!);
        return typeFound!;
    }

    void ThrowIfMultipleTypesFound(Type type, IEnumerable<Type> typesFound)
    {
        if (typesFound.Count() > 1)
        {
            throw new MultipleTypesFound(type, typesFound);
        }
    }

    void ThrowIfTypeNotFound(string fullName, Type typeFound)
    {
        if (typeFound == null)
        {
            throw new UnableToResolveTypeByName(fullName);
        }
    }
}
