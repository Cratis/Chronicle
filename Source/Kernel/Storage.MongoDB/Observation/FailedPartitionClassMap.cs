// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Applications.MongoDB;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// A class map for <see cref="FailedPartition"/>.
/// </summary>
public class FailedPartitionClassMap : IBsonClassMapFor<FailedPartition>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<FailedPartition> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Id);
        classMap.UnmapField(_ => _.LastAttempt);
    }
}
