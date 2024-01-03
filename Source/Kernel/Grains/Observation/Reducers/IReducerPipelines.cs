// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Observation.Reducers;

namespace Aksio.Cratis.Kernel.Grains.Observation.Reducers;

/// <summary>
/// Defines a system that manages <see cref="IReducerPipeline"/> instances.
/// </summary>
public interface IReducerPipelines
{
    /// <summary>
    /// Create a <see cref="IReducerPipeline"/> from a <see cref="ReducerDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="ReducerDefinition"/> to create from.</param>
    /// <returns><see cref="IReducerPipeline"/> instance.</returns>
    Task<IReducerPipeline> GetFor(ReducerDefinition definition);
}
