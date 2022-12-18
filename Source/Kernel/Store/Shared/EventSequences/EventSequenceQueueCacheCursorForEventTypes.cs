// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IQueueCacheCursor"/> for event log filtered for event types.
/// </summary>
public class EventSequenceQueueCacheCursorForEventTypes : EventSequenceQueueCacheCursor
{
    readonly IEnumerable<EventType> _eventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueCacheCursor"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="cursorStart">The start of the cursor.</param>
    /// <param name="streamIdentity"><see cref="IStreamIdentity"/> for the stream.</param>
    /// <param name="storageProvider"><see cref="IEventSequenceStorageProvider"/> to ue for getting events from sequence.</param>
    /// <param name="eventTypes">Collection of <see cref="EventType">Event types</see> to filter the cursor with - default all.</param>
    public EventSequenceQueueCacheCursorForEventTypes(
        IExecutionContextManager executionContextManager,
        EventSequenceNumber cursorStart,
        IStreamIdentity streamIdentity,
        IEventSequenceStorageProvider storageProvider,
        IEnumerable<EventType> eventTypes) : base(executionContextManager, cursorStart, streamIdentity, storageProvider)
    {
        _eventTypes = eventTypes;
    }

    /// <inheritdoc/>
    protected override Task<IEventCursor> GetActualEventCursor(EventSequenceId sequenceId, EventSequenceNumber sequenceNumber) =>
        _storageProvider.GetFromSequenceNumber(sequenceId, sequenceNumber, eventTypes: _eventTypes);
}
