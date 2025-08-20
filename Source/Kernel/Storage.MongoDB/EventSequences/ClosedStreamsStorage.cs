// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="ClosedStreamsStorage"/> for MongoDB.
/// </summary>
/// <param name="database">The <see cref="IMongoDatabase"/>.</param>
public class ClosedStreamsStorage(IEventStoreNamespaceDatabase database) : IClosedStreamsStorage
{

    readonly IMongoCollection<ClosedStreamState> _collection = database.GetClosedStreamCollectionFor(eventSequenceId);

    /// <inheritdoc/>
    public Task<Result<IClosedStreamsStorage.SaveErrors>> Save(ClosedStreamId closedStreamId, ClosedStreamReason reason)
    {
        
    }

    public Task<Option<ClosedStreamState>> Get(ClosedStreamId closedStreamId)
    {
        
    }
}
