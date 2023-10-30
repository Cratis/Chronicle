// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.MongoDB;
using MongoDB.Bson.Serialization;

namespace Aksio.Cratis.Kernel.MongoDB.Jobs;

/// <summary>
/// Represents a class map for <see cref="JobState"/>.
/// </summary>
public class JobStateClassMap : IBsonClassMapFor<JobState>
{
    /// <inheritdoc/>
    public void Configure(BsonClassMap<JobState> classMap)
    {
        classMap.AutoMap();
        classMap.UnmapProperty(_ => _.Remove);
    }
}
