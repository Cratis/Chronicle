// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Types;

/// <summary>
/// Defines a system that handles the relationship between contracts and their implementors.
/// </summary>
/// <remarks>
/// A contract is considered an abstract type or an interface.
/// </remarks>
public interface IContractToImplementorsMap
{
    /// <summary>
    /// Gets the dictionary holding the mapping between contracts and all their implementors.
    /// </summary>
    IDictionary<Type, IEnumerable<Type>> ContractsAndImplementors { get; }

    /// <summary>
    /// Gets all the types in the map.
    /// </summary>
    IEnumerable<Type> All { get; }

    /// <summary>
    /// Feed the map with types.
    /// </summary>
    /// <param name="types"><see cref="IEnumerable{Type}">Types</see> to feed with.</param>
    void Feed(IEnumerable<Type> types);

    /// <summary>
    /// Retrieve implementors of a specific contract.
    /// </summary>
    /// <typeparam name="T">Type of contract to retrieve for.</typeparam>
    /// <returns><see cref="IEnumerable{T}">Types</see> implementing the contract.</returns>
    IEnumerable<Type> GetImplementorsFor<T>();

    /// <summary>
    /// Retrieve implementors of a specific contract.
    /// </summary>
    /// <param name="contract"><see cref="Type"/> of contract to retrieve for.</param>
    /// <returns><see cref="IEnumerable{T}">Types</see> implementing the contract.</returns>
    IEnumerable<Type> GetImplementorsFor(Type contract);
}
