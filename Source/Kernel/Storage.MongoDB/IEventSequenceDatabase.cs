// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

public interface IEventSequenceDatabase
{
    IMongoCollection<ClosedStreamState> GetClosedStreamsCollection(ClosedStreamId closedStreamId);
}