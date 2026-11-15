// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations;

/// <summary>
/// Represents the class map for <see cref="EventSequenceMutationHeadEntry"/>.
/// </summary>
public class EventSequenceMutationHeadEntryClassMap : IBsonClassMapFor<EventSequenceMutationHeadEntry>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<EventSequenceMutationHeadEntry> classMap)
    {
        EventSequenceMutationBsonSerialization.Register();
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.EventSequenceId);
        classMap.MapProperty(_ => _.Coverage).SetDefaultValue(EventSequenceMutationCoverage.Untracked);
        classMap.MapProperty(_ => _.LastAssignedOrdinal).SetDefaultValue(EventSequenceMutationOrdinal.NotSet);
    }
}
