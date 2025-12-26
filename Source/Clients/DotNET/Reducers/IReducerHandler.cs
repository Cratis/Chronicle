// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Defines a system that handle events for a specific reducer.
/// </summary>
public interface IReducerHandler : IHaveReadModel
{
    /// <summary>
    /// Gets the unique identifier of the reducer.
    /// </summary>
    ReducerId Id { get; }

    /// <summary>
    /// Gets the type of the reducer.
    /// </summary>
    Type ReducerType { get; }

    /// <summary>
    /// Gets the event sequence the reducer is reducing from.
    /// </summary>
    EventSequenceId EventSequenceId { get; }

    /// <summary>
    /// Gets the event types for the reducer.
    /// </summary>
    IEnumerable<EventType> EventTypes { get; }

    /// <summary>
    /// Gets whether the reducer should be actively running on the Kernel.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the <see cref="CancellationToken"/> for the handler.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the <see cref="IReducerInvoker"/> that will perform the invocations.
    /// </summary>
    IReducerInvoker Invoker { get; }

    /// <summary>
    /// Handle next events as bulk.
    /// </summary>
    /// <param name="events">Collection of <see cref="AppendedEvent"/> to handle.</param>
    /// <param name="initial">Initial read model value.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> for creating the actual instance of the reducer.</param>
    /// <returns>Reduced read model.</returns>
    Task<ReduceResult> OnNext(IEnumerable<AppendedEvent> events, object? initial, IServiceProvider serviceProvider);

    /// <summary>
    /// Disconnect the handler.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Get the current state of the reducer.
    /// </summary>
    /// <returns>The current <see cref="ReducerState"/>.</returns>
    Task<ReducerState> GetState();

    /// <summary>
    /// Get any failed partitions for a specific reducer.
    /// </summary>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitions();
}
