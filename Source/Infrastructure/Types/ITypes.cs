// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Types;

/// <summary>
/// Defines a system for working with types.
/// </summary>
public interface ITypes
{
    /// <summary>
    /// Gets all assemblies used for type discovery.
    /// </summary>
    IEnumerable<Assembly> Assemblies { get; }

    /// <summary>
    /// Gets returns all collected types.
    /// </summary>
    IEnumerable<Type> All { get; }

    /// <summary>
    /// Find a single implementation of a basetype.
    /// </summary>
    /// <typeparam name="T">Basetype to find for.</typeparam>
    /// <returns>Type found.</returns>
    /// <remarks>
    /// If the base type is an interface, it will look for any types implementing the interface.
    /// If it is a class, it will find anyone inheriting from that class.
    /// </remarks>
    /// <exception cref="MultipleTypesFound">If there is more than one instance found.</exception>
    Type FindSingle<T>();

    /// <summary>
    /// Find multiple implementations of a basetype.
    /// </summary>
    /// <typeparam name="T">Basetype to find for.</typeparam>
    /// <returns>All types implementing or inheriting from the given basetype.</returns>
    /// <remarks>
    /// If the base type is an interface, it will look for any types implementing the interface.
    /// If it is a class, it will find anyone inheriting from that class.
    /// </remarks>
    IEnumerable<Type> FindMultiple<T>();

    /// <summary>
    /// Find a single implementation of a basetype.
    /// </summary>
    /// <param name="type">Basetype to find for.</param>
    /// <returns>Type found.</returns>
    /// <remarks>
    /// If the base type is an interface, it will look for any types implementing the interface.
    /// If it is a class, it will find anyone inheriting from that class.
    /// </remarks>
    /// <exception cref="MultipleTypesFound">If there is more than one instance found.</exception>
    Type FindSingle(Type type);

    /// <summary>
    /// Find multiple implementations of a basetype.
    /// </summary>
    /// <param name="type">Basetype to find for.</param>
    /// <returns>All types implementing or inheriting from the given basetype.</returns>
    /// <remarks>
    /// If the base type is an interface, it will look for any types implementing the interface.
    /// If it is a class, it will find anyone inheriting from that class.
    /// </remarks>
    IEnumerable<Type> FindMultiple(Type type);

    /// <summary>
    /// Find a single type using the full name, without assembly.
    /// </summary>
    /// <param name="fullName">full name of the type to find.</param>
    /// <returns>The type is found, null otherwise.</returns>
    Type FindTypeByFullName(string fullName);
}
