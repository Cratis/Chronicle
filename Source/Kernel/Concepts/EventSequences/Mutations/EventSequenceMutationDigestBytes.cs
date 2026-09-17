// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations;

/// <summary>
/// Provides shared byte operations for event sequence mutation digests.
/// </summary>
static class EventSequenceMutationDigestBytes
{
    /// <summary>
    /// The required digest length in bytes.
    /// </summary>
    internal const int Length = 32;

    /// <summary>
    /// Validates and copies digest bytes.
    /// </summary>
    /// <typeparam name="TDigest">The digest type being constructed.</typeparam>
    /// <param name="bytes">The bytes to validate and copy.</param>
    /// <returns>A defensive copy of the validated bytes.</returns>
    /// <exception cref="InvalidEventSequenceMutationDigestLength">Thrown when <paramref name="bytes"/> does not contain exactly 32 bytes.</exception>
    internal static byte[] CopyAndValidate<TDigest>(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Length)
        {
            throw new InvalidEventSequenceMutationDigestLength(typeof(TDigest), bytes.Length);
        }

        return bytes.ToArray();
    }

    /// <summary>
    /// Calculates a stable content hash code for digest bytes.
    /// </summary>
    /// <param name="bytes">The bytes to hash.</param>
    /// <returns>The stable hash code.</returns>
    internal static int GetStableHashCode(ReadOnlySpan<byte> bytes)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;
        var hash = offsetBasis;
        unchecked
        {
            foreach (var value in bytes)
            {
                hash = (hash ^ value) * prime;
            }
        }

        return (int)hash;
    }
}
