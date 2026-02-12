// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Storage.Jobs;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Jobs;

/// <summary>
/// Represents the class map for <see cref="JobState"/>.
/// </summary>
public class JobStateClassMap : IBsonClassMapFor<JobState>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<JobState> classMap)
    {
        classMap.AutoMap();
        classMap.MapIdProperty(_ => _.Id);

        // Unmap the Request property to prevent MongoDB from using discriminator-based serialization.
        // The JobStateSerializer handles complete serialization/deserialization using IJobTypes.
        classMap.UnmapMember(_ => _.Request);
    }
}
