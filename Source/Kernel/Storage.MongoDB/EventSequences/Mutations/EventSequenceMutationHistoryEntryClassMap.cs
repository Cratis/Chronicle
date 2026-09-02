// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations;

/// <summary>
/// Represents the class map for <see cref="EventSequenceMutationHistoryEntry"/>.
/// </summary>
public class EventSequenceMutationHistoryEntryClassMap : IBsonClassMapFor<EventSequenceMutationHistoryEntry>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<EventSequenceMutationHistoryEntry> classMap)
    {
        EventSequenceMutationBsonSerialization.Register();
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.MutationId);
    }
}
