// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Projections;

namespace Aksio.Cratis.Kernel.Engines.Observation.Reducers;

/// <summary>
/// Defines a system that can coordinate the effort around reducers.
/// </summary>
public interface IReducerPipeline
{
    /// <summary>
    /// Gets the <see cref="Model"/> the pipeline is for.
    /// </summary>
    Model ReadModel { get; }

    /// <summary>
    /// Gets the <see cref="IProjectionSink">sink</see> to use for output.
    /// </summary>
    IProjectionSink Sink { get; }

    /// <summary>
    /// Handles the event and coordinates everything according to the pipeline.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to handle.</param>
    /// <param name="reducer"><see cref="ReducerDelegate"/> delegate.</param>
    /// <returns>Awaitable task.</returns>
    Task Handle(AppendedEvent @event, ReducerDelegate reducer);
}
