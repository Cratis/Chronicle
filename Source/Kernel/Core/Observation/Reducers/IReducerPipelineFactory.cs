// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Storage.ReadModels;

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

    /// <summary>
    /// Create a pipeline whose sink addresses only this replay attempt's target.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="definition">The reducer definition.</param>
    /// <param name="context">The isolated replay target.</param>
    /// <returns>The replay pipeline.</returns>
    /// <exception cref="ReplayInitializationFailed">The factory does not support isolated replay pipelines.</exception>
    Task<IReducerPipeline> CreateForReplay(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReducerDefinition definition, ReplayContext context) =>
        throw new ReplayInitializationFailed(ICanHandleReplayForObserver.Error.CannotHandle);
}
