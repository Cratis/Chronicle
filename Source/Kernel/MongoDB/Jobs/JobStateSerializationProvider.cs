// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Json;
using MongoDB.Bson.Serialization;

namespace Aksio.Cratis.Kernel.MongoDB.Jobs;

/// <summary>
/// Represents a <see cref="IBsonSerializationProvider"/> for <see cref="JobState"/>.
/// </summary>
public class JobStateSerializationProvider : IBsonSerializationProvider
{
    /// <inheritdoc/>
    public IBsonSerializer GetSerializer(Type type)
    {
        if (type.IsAssignableTo(typeof(JobState)))
        {
            return new JobStateSerializer(Globals.JsonSerializerOptions);
        }

        return null!;
    }
}
