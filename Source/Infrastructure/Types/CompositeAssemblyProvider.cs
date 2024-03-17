// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Types;

/// <summary>
/// Represents an implementation of <see cref="ICanProvideAssembliesForDiscovery"/> that provides a composite of multiple <see cref="ICanProvideAssembliesForDiscovery"/>.
/// </summary>
public class CompositeAssemblyProvider : ICanProvideAssembliesForDiscovery
{
    readonly List<ICanProvideAssembliesForDiscovery> _providers = [];

    /// <summary>
    /// Initializes a new instance of <see cref="CompositeAssemblyProvider"/>.
    /// </summary>
    /// <param name="providers">Providers to use.</param>
    public CompositeAssemblyProvider(params ICanProvideAssembliesForDiscovery[] providers)
    {
        _providers.AddRange(providers);
    }

    /// <inheritdoc/>
    public IEnumerable<Assembly> Assemblies => _providers.SelectMany(_ => _.Assemblies);

    /// <inheritdoc/>
    public IEnumerable<Type> DefinedTypes => _providers.SelectMany(_ => _.DefinedTypes);

    /// <inheritdoc/>
    public void Initialize()
    {
        _providers.ForEach(_ => _.Initialize());
    }
}
