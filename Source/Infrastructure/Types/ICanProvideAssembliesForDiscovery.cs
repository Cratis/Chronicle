// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Types;

/// <summary>
/// Defines a system that can provide assemblies that can be used for type discovery.
/// </summary>
public interface ICanProvideAssembliesForDiscovery
{
    /// <summary>
    /// Gets the assemblies to use for type discovery.
    /// </summary>
    IEnumerable<Assembly> Assemblies { get; }

    /// <summary>
    /// Gets all the defined types from all the assemblies.
    /// </summary>
    IEnumerable<Type> DefinedTypes { get; }

    /// <summary>
    /// Initializes the system.
    /// </summary>
    void Initialize();
}
