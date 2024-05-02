// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.ProxyGenerator.Templates;

/// <summary>
/// Defines a base for a descriptor.
/// </summary>
public interface IDescriptor
{
    /// <summary>
    /// Gets the type the descriptor represents.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets the name that represents the type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a collection of types involved in the command.
    /// </summary>
    IEnumerable<Type> TypesInvolved { get; }
}
