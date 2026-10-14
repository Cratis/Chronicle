// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Produces length-safe physical database names without splitting Unicode characters.
/// </summary>
internal static class DatabaseName
{
    /// <summary>
    /// Gets the provider's identifier limit.
    /// </summary>
    /// <param name="provider">The SQL provider.</param>
    /// <returns>The byte or code-unit limit.</returns>
    internal static int Limit(DatabaseType provider) => provider == DatabaseType.PostgreSql ? 63 : 128;

    /// <summary>
    /// Tests whether the original name fits unchanged.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="provider">The SQL provider.</param>
    /// <returns>Whether the name fits.</returns>
    internal static bool Fits(string name, DatabaseType provider) => Size(name, provider) <= Limit(provider);

    /// <summary>
    /// Keeps a short name unchanged and hashes a long name.
    /// </summary>
    /// <param name="name">The full logical name.</param>
    /// <param name="provider">The SQL provider.</param>
    /// <returns>The physical name.</returns>
    internal static string Physical(string name, DatabaseType provider) => Fits(name, provider) ? name : Hashed(name, provider);

    /// <summary>
    /// Adds a stable SHA-256 prefix suffix to a readable character-boundary prefix.
    /// </summary>
    /// <param name="name">The full logical name.</param>
    /// <param name="provider">The SQL provider.</param>
    /// <returns>The shortened name.</returns>
    internal static string Hashed(string name, DatabaseType provider)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..16];
        return $"{Clip(name, provider, Limit(provider) - 17)}~{hash}";
    }

    /// <summary>
    /// Clips an identifier without splitting a Unicode scalar value.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="provider">The SQL provider.</param>
    /// <param name="limit">The allowed byte or code-unit count.</param>
    /// <returns>The clipped name.</returns>
    internal static string Clip(string name, DatabaseType provider, int limit)
    {
        var result = new StringBuilder();
        var size = 0;
        foreach (var rune in name.EnumerateRunes())
        {
            var next = provider == DatabaseType.PostgreSql ? rune.Utf8SequenceLength : rune.Utf16SequenceLength;
            if (size + next > limit)
            {
                break;
            }

            result.Append(rune.ToString());
            size += next;
        }

        return result.ToString();
    }

    static int Size(string name, DatabaseType provider) => provider == DatabaseType.PostgreSql ? Encoding.UTF8.GetByteCount(name) : name.Length;
}
