// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IClosedStreamsConstraintStorage"/> for MongoDB.
/// </summary>
/// <param name="eventStoreNamespaceDatabase"><see cref="IEventStoreNamespaceDatabase"/> for the storage.</param>
/// <param name="eventSequenceId"><see cref="EventSequenceId"/> for the storage.</param>
public class ClosedStreamsConstraintStorage(IEventStoreNamespaceDatabase eventStoreNamespaceDatabase, EventSequenceId eventSequenceId) : IClosedStreamsConstraintStorage
{
    readonly IMongoCollection<ClosedStreamDocument> _collection =
        eventStoreNamespaceDatabase.GetCollection<ClosedStreamDocument>($"{eventSequenceId}+closed_streams");

    /// <inheritdoc/>
    public async Task<bool> IsStreamClosed(EventStreamType streamType, EventStreamId streamId)
    {
        using var result = await _collection.FindAsync(_ => _.StreamType == streamType.Value && _.StreamId == streamId.Value);
        return await result.AnyAsync();
    }

    /// <inheritdoc/>
    public async Task CloseStream(EventStreamType streamType, EventStreamId streamId)
    {
        await _collection.ReplaceOneAsync(
            _ => _.StreamType == streamType.Value && _.StreamId == streamId.Value,
            new ClosedStreamDocument(streamType.Value, streamId.Value),
            new ReplaceOptions { IsUpsert = true });
    }
}
