// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Reducers;

namespace Cratis.Chronicle.Grains.Observation.Reducers;

/// <summary>
/// Defines a system for working with <see cref="ReducerDefinition"/>.
/// </summary>
public interface IReducerPipelineDefinitions
{
    /// <summary>
    /// Register <see cref="ReducerDefinition"/> in the system.
    /// </summary>
    /// <param name="definition"><see cref="ReducerDefinition"/> to register.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(ReducerDefinition definition);

    /// <summary>
    /// Check if a reducer has a definition, based on its identifier.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    Task<bool> HasFor(ReducerId reducerId);

    /// <summary>
    /// Get the <see cref="ReducerDefinition"/> based on its identifier.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> to get for.</param>
    /// <returns>The <see cref="ReducerDefinition"/> instance.</returns>
    Task<ReducerDefinition> GetFor(ReducerId reducerId);
}
