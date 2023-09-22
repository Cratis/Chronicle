// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;
using Aksio.MongoDB;
using MongoDB.Bson.Serialization;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

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
    }
}
