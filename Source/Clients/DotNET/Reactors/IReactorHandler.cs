// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Defines a system that handle events for a specific reactor.
/// </summary>
public interface IReactorHandler
{
    /// <summary>
    /// Gets the unique identifier of the Reactor.
    /// </summary>
    ReactorId Id { get; }

    /// <summary>
    /// Gets the event log for the Reactor.
    /// </summary>
    EventSequenceId EventSequenceId { get; }

    /// <summary>
    /// Gets the event types for the Reactor.
    /// </summary>
    IEnumerable<EventType> EventTypes { get; }

    /// <summary>
    /// Gets the <see cref="CancellationToken"/> for the handler.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Handle next event.
    /// </summary>
    /// <param name="metadata"><see cref="EventMetadata"/> for the event.</param>
    /// <param name="context"><see cref="EventContext"/> for the event.</param>
    /// <param name="content">Actual content.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> for creating the actual instance of the reactor.</param>
    /// <returns>Awaitable task.</returns>
    Task OnNext(EventMetadata metadata, EventContext context, object content, IServiceProvider serviceProvider);

    /// <summary>
    /// Get the current state of the Reactor.
    /// </summary>
    /// <returns>Current <see cref="ReactorState"/>.</returns>
    Task<ReactorState> GetState();

    /// <summary>
    /// Get any failed partitions for a specific reactor.
    /// </summary>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitions();

    /// <summary>
    /// Disconnect the handler.
    /// </summary>
    void Disconnect();
}
