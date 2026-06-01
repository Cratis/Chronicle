// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams;

/// <summary>
/// Represents an implementation of <see cref="IClosedStreamsConstraintStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the storage is for.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the storage is for.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the storage is for.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for operations.</param>
public class ClosedStreamsConstraintStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, IDatabase database) : IClosedStreamsConstraintStorage
{
    /// <inheritdoc/>
    public async Task<bool> IsStreamClosed(EventStreamType streamType, EventStreamId streamId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        return await scope.DbContext.ClosedStreams
            .AnyAsync(e => e.EventSequenceId == eventSequenceId.Value &&
                           e.StreamType == streamType.Value &&
                           e.StreamId == streamId.Value);
    }

    /// <inheritdoc/>
    public async Task CloseStream(EventStreamType streamType, EventStreamId streamId)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var existing = await scope.DbContext.ClosedStreams
            .FirstOrDefaultAsync(e => e.EventSequenceId == eventSequenceId.Value &&
                                      e.StreamType == streamType.Value &&
                                      e.StreamId == streamId.Value);
        if (existing is null)
        {
            scope.DbContext.ClosedStreams.Add(new ClosedStreamEntry
            {
                EventSequenceId = eventSequenceId.Value,
                StreamType = streamType.Value,
                StreamId = streamId.Value
            });
            await scope.DbContext.SaveChangesAsync();
        }
    }
}
