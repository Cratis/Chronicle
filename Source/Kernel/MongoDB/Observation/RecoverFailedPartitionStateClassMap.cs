// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable
using Aksio.Cratis.Extensions.MongoDB;
using Aksio.Cratis.Kernel.Grains.Observation;
using MongoDB.Bson.Serialization;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

/// <summary>
/// A class map for <see cref="RecoverFailedPartitionState"/>.
/// </summary>
public class RecoverFailedPartitionStateClassMap : IBsonClassMapFor<RecoverFailedPartitionState>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<RecoverFailedPartitionState> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Id);
    }
}