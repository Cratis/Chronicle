// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// A class map for <see cref="ObserverPartitionCounts"/>.
/// </summary>
public class ObserverPartitionCountsClassMap : IBsonClassMapFor<ObserverPartitionCounts>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<ObserverPartitionCounts> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Id);
    }
}
