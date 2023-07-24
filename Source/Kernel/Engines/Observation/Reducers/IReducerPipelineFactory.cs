// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Observation.Reducers;

namespace Aksio.Cratis.Kernel.Engines.Observation.Reducers;

/// <summary>
/// Defines a system that can create <see cref="IReducerPipeline"/> instances.
/// </summary>
public interface IReducerPipelineFactory
{
    /// <summary>
    /// Create a <see cref="IReducerPipeline"/> from a <see cref="ReducerDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="ReducerDefinition"/> to create from.</param>
    /// <returns><see cref="IReducerPipeline"/> instance.</returns>
    Task<IReducerPipeline> CreateFrom(ReducerDefinition definition);
}
