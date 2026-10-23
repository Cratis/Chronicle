// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Assigns event sources to stable replay buckets.
/// </summary>
public static class ReplayBuckets
{
    /// <summary>
    /// Assigns an event source using version 1: FNV-1a 64-bit over RFC/network-order Guid bytes,
    /// or ordinal UTF-8 bytes for identifiers that are not Guids.
    /// </summary>
    /// <param name="eventSourceId">The event source to assign.</param>
    /// <param name="count">The number of buckets, from 1 through 64.</param>
    /// <returns>The event source's bucket.</returns>
    /// <exception cref="ReplayBucketCountOutOfRange">The count is outside the supported range.</exception>
    public static ReplayBucket For(EventSourceId eventSourceId, int count)
    {
        if (count is < 1 or > 64)
        {
            throw new ReplayBucketCountOutOfRange(count);
        }

        var bytes = Guid.TryParse(eventSourceId.Value, out var guid)
            ? guid.ToByteArray(bigEndian: true)
            : Encoding.UTF8.GetBytes(eventSourceId.Value);
        return new((int)(Hash(bytes) % (ulong)count), count, 1);
    }

    static ulong Hash(ReadOnlySpan<byte> bytes)
    {
        var hash = 14695981039346656037UL;
        foreach (var value in bytes)
        {
            hash = unchecked((hash ^ value) * 1099511628211UL);
        }

        return hash;
    }
}
