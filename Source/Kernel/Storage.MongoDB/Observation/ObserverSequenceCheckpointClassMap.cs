// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents the BSON mapping for parked observer checkpoints.
/// </summary>
public class ObserverSequenceCheckpointClassMap : IBsonClassMapFor<ObserverSequenceCheckpoint>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<ObserverSequenceCheckpoint> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Id);
        classMap.MapMember(_ => _.PartitionClaims).SetIsRequired(false).SetIgnoreIfNull(true);
        classMap.MapMember(_ => _.ObserverWideClaim).SetIsRequired(false).SetIgnoreIfNull(true);
        classMap.MapMember(_ => _.DeliveryFences).SetIsRequired(false).SetIgnoreIfNull(true);
    }
}
