// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Defines a system that manages <see cref="IReducerPipeline"/> instances.
/// </summary>
public interface IReducerPipelineFactory
{
    /// <summary>
    /// Create a <see cref="IReducerPipeline"/> from a <see cref="ReducerDefinition"/>.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the pipeline is for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the pipeline is for.</param>
    /// <param name="definition"><see cref="ReducerDefinition"/> to create from.</param>
    /// <returns><see cref="IReducerPipeline"/> instance.</returns>
    Task<IReducerPipeline> Create(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReducerDefinition definition);
}
