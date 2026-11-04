// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Computes HMAC-SHA256 either one-shot from a key or with reusable HMAC state.
/// </summary>
internal static class UniqueConstraintHmac
{
    /// <summary>
    /// Computes the HMAC of the data.
    /// </summary>
    /// <param name="key">The key, used when no reusable state is given.</param>
    /// <param name="hmac">The reusable state, or null to use the key.</param>
    /// <param name="data">The data to authenticate.</param>
    /// <param name="destination">Receives the 32-byte hash.</param>
    internal static void Compute(ReadOnlySpan<byte> key, IncrementalHash? hmac, ReadOnlySpan<byte> data, Span<byte> destination)
    {
        if (hmac is null)
        {
            HMACSHA256.HashData(key, data, destination);
            return;
        }
        hmac.AppendData(data);
        hmac.GetHashAndReset(destination);
    }
}
