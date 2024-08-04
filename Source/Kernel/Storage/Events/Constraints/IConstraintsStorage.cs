// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Defines the storage mechanism for constraints.
/// </summary>
public interface IConstraintsStorage
{
    /// <summary>
    /// Get all definitions.
    /// </summary>
    /// <returns>Collection of <see cref="IConstraintDefinition"/>.</returns>
    Task<IEnumerable<IConstraintDefinition>> GetDefinitions();

    /// <summary>
    /// Save a definition.
    /// </summary>
    /// <param name="definition"><see cref="IConstraintDefinition"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task SaveDefinition(IConstraintDefinition definition);
}
