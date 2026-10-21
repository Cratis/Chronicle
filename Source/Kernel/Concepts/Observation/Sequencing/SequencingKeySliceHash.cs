// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Numerics;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Computes the frozen xxHash32 algorithm with seed zero for sequencing key slices.
/// </summary>
internal static class SequencingKeySliceHash
{
    const uint Prime1 = 2654435761;
    const uint Prime2 = 2246822519;
    const uint Prime3 = 3266489917;
    const uint Prime4 = 668265263;
    const uint Prime5 = 374761393;

    /// <summary>
    /// Hashes bytes without allocating, using little-endian lanes and unsigned wraparound.
    /// </summary>
    /// <param name="bytes">Bytes to hash.</param>
    /// <returns>The xxHash32 value with seed zero.</returns>
    internal static uint Hash(ReadOnlySpan<byte> bytes)
    {
        unchecked
        {
            var offset = 0;
            var hash = Prime5;
            if (bytes.Length >= 16)
            {
                var first = Prime1 + Prime2;
                var second = Prime2;
                var third = 0U;
                var fourth = 0U - Prime1;
                do
                {
                    first = Round(first, BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]));
                    second = Round(second, BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 4)..]));
                    third = Round(third, BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 8)..]));
                    fourth = Round(fourth, BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 12)..]));
                    offset += 16;
                }
                while (offset <= bytes.Length - 16);

                hash = BitOperations.RotateLeft(first, 1) + BitOperations.RotateLeft(second, 7)
                    + BitOperations.RotateLeft(third, 12) + BitOperations.RotateLeft(fourth, 18);
            }

            hash += (uint)bytes.Length;
            while (offset <= bytes.Length - 4)
            {
                hash += BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]) * Prime3;
                hash = BitOperations.RotateLeft(hash, 17) * Prime4;
                offset += 4;
            }

            while (offset < bytes.Length)
            {
                hash += bytes[offset++] * Prime5;
                hash = BitOperations.RotateLeft(hash, 11) * Prime1;
            }

            hash ^= hash >> 15;
            hash *= Prime2;
            hash ^= hash >> 13;
            hash *= Prime3;
            hash ^= hash >> 16;

            return hash;
        }
    }

    static uint Round(uint accumulator, uint lane) => unchecked(BitOperations.RotateLeft(accumulator + (lane * Prime2), 13) * Prime1);
}
