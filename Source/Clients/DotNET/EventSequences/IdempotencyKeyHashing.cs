// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Computes the length-prefixed digest used to derive keys. It must stay identical to the kernel's publication identity encoding.
/// </summary>
internal static class IdempotencyKeyHashing
{
    /// <summary>
    /// Hashes the parts into an upper-case hexadecimal SHA-256 digest.
    /// </summary>
    /// <param name="parts">The parts to hash, each length-prefixed.</param>
    /// <returns>The 64 character digest.</returns>
    internal static string Hash(params string[] parts)
    {
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            builder.Append(part.Length).Append(':').Append(part).Append('|');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}
