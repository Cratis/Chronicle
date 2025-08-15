// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Grains.Observation.Reducers;

/// <summary>
/// Defines a system that can coordinate the effort around reducers.
/// </summary>
public interface IReducerPipeline
{
    /// <summary>
    /// Gets the <see cref="Concepts.ReadModels.ReadModelDefinition"/> the pipeline is for.
    /// </summary>
    ReadModelDefinition ReadModel { get; }

    /// <summary>
    /// Gets the <see cref="ISink">sink</see> to use for output.
    /// </summary>
    ISink Sink { get; }

    /// <summary>
    /// Notifies about the beginning of a replay.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>Awaitable task.</returns>
    Task BeginReplay(ReplayContext context);

    /// <summary>
    /// Notifies about the end of a replay.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>Awaitable task.</returns>
    Task EndReplay(ReplayContext context);

    /// <summary>
    /// Handles the event and coordinates everything according to the pipeline.
    /// </summary>
    /// <param name="context">The <ee cref="ReducerContext"/> being reduced.</param>
    /// <param name="reducer"><see cref="ReducerDelegate"/> delegate.</param>
    /// <returns>Awaitable task.</returns>
    Task Handle(ReducerContext context, ReducerDelegate reducer);
}
