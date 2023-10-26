// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.MongoDB.Grains;
using Aksio.MongoDB;
using MongoDB.Bson.Serialization;

namespace Aksio.Cratis.Kernel.MongoDB.Jobs;

/// <summary>
/// Represents a class map for <see cref="JobStepState"/>.
/// </summary>
public class JobStepStateClassMap : IBsonClassMapFor<JobStepState>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<JobStepState> classMap)
    {
        classMap.AutoMap();
        classMap.MapField(_ => _.GrainId).SetSerializer(new GrainIdSerializer());
    }
}
