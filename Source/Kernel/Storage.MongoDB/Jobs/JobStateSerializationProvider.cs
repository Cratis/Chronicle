// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Jobs;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Jobs;

/// <summary>
/// Represents a <see cref="IBsonSerializationProvider"/> for <see cref="JobState"/> and its derived types.
/// </summary>
/// <param name="jobStateSerializer">The <see cref="JobStateSerializer"/> instance.</param>
public class JobStateSerializationProvider(JobStateSerializer jobStateSerializer) : IBsonSerializationProvider
{
    /// <inheritdoc/>
    public IBsonSerializer? GetSerializer(Type type)
    {
        if (type == typeof(JobState))
        {
            return jobStateSerializer;
        }

        if (type.IsSubclassOf(typeof(JobState)))
        {
            var wrapperType = typeof(JobStateSerializerWrapper<>).MakeGenericType(type);
            return (IBsonSerializer)Activator.CreateInstance(wrapperType, jobStateSerializer)!;
        }

        return null;
    }
}
